using FluentValidation;
using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LexisCampusDMS.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IHashService _hashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<UploadDocumentRequestDto> _uploadValidator;
    private readonly IValidator<RectifyDocumentRequestDto> _rectifyValidator;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IUnitOfWork unitOfWork,
        IDocumentRepository documentRepository,
        IStorageService storageService,
        IHashService hashService,
        ICurrentUserService currentUserService,
        IValidator<UploadDocumentRequestDto> uploadValidator,
        IValidator<RectifyDocumentRequestDto> rectifyValidator,
        ILogger<DocumentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _uploadValidator = uploadValidator ?? throw new ArgumentNullException(nameof(uploadValidator));
        _rectifyValidator = rectifyValidator ?? throw new ArgumentNullException(nameof(rectifyValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<DocumentResponseDto>> UploadDocumentAsync(
        UploadDocumentRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // 1. Validate request metadata & file parameters
        var validationResult = await _uploadValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Validation failed for document upload request: {Errors}", string.Join("; ", validationErrors));
            return Result<DocumentResponseDto>.Failure(
                "Los datos del documento son inválidos.",
                "VALIDATION_ERROR",
                validationErrors);
        }

        // 2. Compute non-blocking SHA-256 hash over input stream
        string fileHashSha256;
        try
        {
            fileHashSha256 = await _hashService.ComputeSha256Async(request.FileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute SHA-256 hash for document '{FileName}'", request.FileName);
            return Result<DocumentResponseDto>.Failure(
                "No fue posible procesar la integridad criptográfica del archivo.",
                "HASH_COMPUTATION_ERROR");
        }

        // 3. Construct hierarchical storage path and upload to MinIO S3
        var sanitizedRegistration = request.StudentRegistration.Trim();
        var sanitizedFileName = Path.GetFileName(request.FileName);
        var storagePath = $"{sanitizedRegistration}/{DateTime.UtcNow.Year}/{fileHashSha256}_{sanitizedFileName}";

        string persistedStoragePath;
        try
        {
            persistedStoragePath = await _storageService.UploadFileAsync(
                request.FileStream,
                storagePath,
                request.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to storage at path '{StoragePath}'", storagePath);
            return Result<DocumentResponseDto>.Failure(
                "Error al almacenar el archivo físico en el repositorio de objetos.",
                "STORAGE_UPLOAD_ERROR");
        }

        // 4 & 5. Atomic SQL Server Transaction (Document + DocumentVersion v1 + AuditLog)
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentUserId = string.IsNullOrWhiteSpace(_currentUserService.UserId) ? "System" : _currentUserService.UserId;
            var currentUserIp = _currentUserService.IpAddress;

            // Create Document entity
            var document = new Document(
                request.Title,
                sanitizedRegistration,
                request.DocumentType,
                currentUserId);

            // Add DocumentVersion v1
            document.AddNewVersion(
                persistedStoragePath,
                fileHashSha256,
                request.FileSizeBytes,
                request.ContentType,
                currentUserId);

            // Add AuditLog entry
            var auditLog = new AuditLog(
                currentUserId,
                AuditAction.DocumentUploaded,
                document.Id,
                currentUserIp,
                $"DOCUMENT_UPLOADED: Documento '{document.Title}' matricula '{document.StudentRegistration}' registrado con versión 1. Hash: {fileHashSha256}.");

            await _documentRepository.RegisterDocumentAsync(document, cancellationToken);
            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Document '{DocumentId}' with version 1 successfully registered by user '{UserId}'",
                document.Id,
                currentUserId);

            var responseDto = new DocumentResponseDto
            {
                Id = document.Id,
                Title = document.Title,
                StudentRegistration = document.StudentRegistration,
                DocumentType = document.DocumentType,
                Status = document.Status,
                CurrentVersion = document.CurrentVersion,
                CreatedAtUtc = document.CreatedAtUtc,
                CurrentFileHash = fileHashSha256,
                CreatedBy = document.CreatedBy
            };

            return Result<DocumentResponseDto>.Success(
                responseDto,
                "Documento y versión inicial cargados exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed registering document '{Title}'. Initiating rollback and storage compensation.", request.Title);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            // Storage Compensation: Delete uploaded object from MinIO to prevent orphaned files
            try
            {
                await _storageService.DeleteFileAsync(persistedStoragePath, CancellationToken.None);
                _logger.LogInformation("Compensated storage upload by deleting orphaned object '{StoragePath}'", persistedStoragePath);
            }
            catch (Exception compEx)
            {
                _logger.LogError(compEx, "Storage compensation failed for object '{StoragePath}'", persistedStoragePath);
            }

            return Result<DocumentResponseDto>.Failure(
                "No fue posible guardar el registro del documento en la base de datos.",
                "PERSISTENCE_TRANSACTION_FAILED");
        }
    }

    public async Task<Result<DocumentResponseDto>> RectifyDocumentAsync(
        RectifyDocumentRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // 1. Validate request metadata & file parameters
        var validationResult = await _rectifyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Validation failed for document rectification request: {Errors}", string.Join("; ", validationErrors));
            return Result<DocumentResponseDto>.Failure(
                "Los datos para la rectificación del documento son inválidos.",
                "VALIDATION_ERROR",
                validationErrors);
        }

        // 2. Locate existing document by DocumentId or by StudentRegistration + DocumentType
        Document? document = null;
        if (request.DocumentId.HasValue && request.DocumentId.Value != Guid.Empty)
        {
            document = await _documentRepository.GetWithDetailsAsync(request.DocumentId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.StudentRegistration) && request.DocumentType.HasValue)
        {
            document = await _documentRepository.GetByRegistrationAndTypeWithDetailsAsync(
                request.StudentRegistration, 
                request.DocumentType.Value, 
                cancellationToken);
        }

        if (document is null)
        {
            _logger.LogWarning("Document not found for rectification: DocumentId={DocumentId}, StudentRegistration={StudentRegistration}",
                request.DocumentId, request.StudentRegistration);
            return Result<DocumentResponseDto>.Failure(
                "El documento especificado para rectificación no fue encontrado.",
                "DOCUMENT_NOT_FOUND");
        }

        // 3. Compute non-blocking SHA-256 hash over rectified file stream
        string fileHashSha256;
        try
        {
            fileHashSha256 = await _hashService.ComputeSha256Async(request.FileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute SHA-256 hash for rectified file '{FileName}'", request.FileName);
            return Result<DocumentResponseDto>.Failure(
                "No fue posible procesar la integridad criptográfica del archivo rectificado.",
                "HASH_COMPUTATION_ERROR");
        }

        // 4. Construct hierarchical storage path with next version
        var nextVersionNumber = (document.Versions.Count > 0 ? document.Versions.Max(v => v.VersionNumber) : 0) + 1;
        var sanitizedRegistration = document.StudentRegistration.Trim();
        var sanitizedFileName = Path.GetFileName(request.FileName);
        var storagePath = $"{sanitizedRegistration}/{DateTime.UtcNow.Year}/v{nextVersionNumber}_{fileHashSha256}_{sanitizedFileName}";

        // 5. Upload new binary version to MinIO S3
        string persistedStoragePath;
        try
        {
            persistedStoragePath = await _storageService.UploadFileAsync(
                request.FileStream,
                storagePath,
                request.ContentType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload rectified file to storage at path '{StoragePath}'", storagePath);
            return Result<DocumentResponseDto>.Failure(
                "Error al almacenar el archivo rectificado en el repositorio de objetos.",
                "STORAGE_UPLOAD_ERROR");
        }

        // 6. Atomic SQL Server Transaction: Link DocumentVersion, update CurrentVersion, and AuditLog
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var currentUserId = string.IsNullOrWhiteSpace(_currentUserService.UserId) ? "System" : _currentUserService.UserId;
            var currentUserIp = _currentUserService.IpAddress;

            // Inmutable version addition: prior versions are preserved untouched
            document.AddNewVersion(
                persistedStoragePath,
                fileHashSha256,
                request.FileSizeBytes,
                request.ContentType,
                currentUserId);

            // AuditLog with DocumentRectified action and mandatory change reason
            var auditLog = new AuditLog(
                currentUserId,
                AuditAction.DocumentRectified,
                document.Id,
                currentUserIp,
                $"DOCUMENT_RECTIFIED: Versión {document.CurrentVersion} generada para documento '{document.Title}'. Motivo: {request.ChangeReason.Trim()}. Hash: {fileHashSha256}.");

            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Document '{DocumentId}' successfully rectified to version {Version} by user '{UserId}'. Reason: {Reason}",
                document.Id,
                document.CurrentVersion,
                currentUserId,
                request.ChangeReason);

            var responseDto = new DocumentResponseDto
            {
                Id = document.Id,
                Title = document.Title,
                StudentRegistration = document.StudentRegistration,
                DocumentType = document.DocumentType,
                Status = document.Status,
                CurrentVersion = document.CurrentVersion,
                CreatedAtUtc = document.CreatedAtUtc,
                CurrentFileHash = fileHashSha256,
                CreatedBy = document.CreatedBy,
                Versions = document.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new DocumentVersionDto
                {
                    VersionNumber = v.VersionNumber,
                    FileHash = v.FileHashSha256,
                    FileSizeBytes = v.FileSize,
                    CreatedAtUtc = v.CreatedAtUtc,
                    UploadedBy = v.CreatedByUserId,
                    MimeType = v.MimeType,
                    StoragePath = v.StoragePath
                }).ToList()
            };

            return Result<DocumentResponseDto>.Success(
                responseDto,
                $"Documento rectificado exitosamente. Se ha generado la versión {document.CurrentVersion}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed rectifying document '{DocumentId}'. Initiating rollback and storage compensation.", document.Id);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            // Storage Compensation: Remove newly uploaded blob from MinIO
            try
            {
                await _storageService.DeleteFileAsync(persistedStoragePath, CancellationToken.None);
                _logger.LogInformation("Compensated storage upload by deleting orphaned rectified object '{StoragePath}'", persistedStoragePath);
            }
            catch (Exception compEx)
            {
                _logger.LogError(compEx, "Storage compensation failed for rectified object '{StoragePath}'", persistedStoragePath);
            }

            return Result<DocumentResponseDto>.Failure(
                "No fue posible guardar la nueva versión del documento en la base de datos.",
                "PERSISTENCE_TRANSACTION_FAILED");
        }
    }

    public async Task<Result<DocumentResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetWithDetailsAsync(id, cancellationToken);
        if (document is null)
        {
            return Result<DocumentResponseDto>.Failure(
                $"El documento con ID '{id}' no fue encontrado.",
                "DOCUMENT_NOT_FOUND");
        }

        var latestVersion = document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        var dto = new DocumentResponseDto
        {
            Id = document.Id,
            Title = document.Title,
            StudentRegistration = document.StudentRegistration,
            DocumentType = document.DocumentType,
            Status = document.Status,
            CurrentVersion = document.CurrentVersion,
            CreatedAtUtc = document.CreatedAtUtc,
            CurrentFileHash = latestVersion?.FileHashSha256 ?? string.Empty,
            CreatedBy = document.CreatedBy,
            Versions = document.Versions.Select(v => new DocumentVersionDto
            {
                VersionNumber = v.VersionNumber,
                FileHash = v.FileHashSha256,
                FileSizeBytes = v.FileSize,
                CreatedAtUtc = v.CreatedAtUtc,
                UploadedBy = v.CreatedByUserId,
                MimeType = v.MimeType,
                StoragePath = v.StoragePath
            }).ToList()
        };

        return Result<DocumentResponseDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<DocumentResponseDto>>> GetByStudentRegistrationAsync(
        string studentRegistration, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentRegistration))
        {
            return Result<IReadOnlyList<DocumentResponseDto>>.Failure(
                "La matrícula del estudiante es requerida.",
                "INVALID_STUDENT_REGISTRATION");
        }

        var documents = await _documentRepository.GetByStudentRegistrationAsync(studentRegistration, cancellationToken);

        var dtos = documents.Select(d =>
        {
            var latestVersion = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            return new DocumentResponseDto
            {
                Id = d.Id,
                Title = d.Title,
                StudentRegistration = d.StudentRegistration,
                DocumentType = d.DocumentType,
                Status = d.Status,
                CurrentVersion = d.CurrentVersion,
                CreatedAtUtc = d.CreatedAtUtc,
                CurrentFileHash = latestVersion?.FileHashSha256 ?? string.Empty,
                CreatedBy = d.CreatedBy
            };
        }).ToList();

        return Result<IReadOnlyList<DocumentResponseDto>>.Success(dtos);
    }

    public async Task<PagedResult<DocumentResponseDto>> SearchAsync(
        SearchFilterDto filter, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));

        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

        var (items, totalCount) = await _documentRepository.SearchAsync(
            filter.StudentRegistration,
            filter.DocumentType,
            filter.FromDateUtc,
            filter.ToDateUtc,
            pageNumber,
            pageSize,
            cancellationToken);

        var dtos = items.Select(d =>
        {
            var latestVersion = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            return new DocumentResponseDto
            {
                Id = d.Id,
                Title = d.Title,
                StudentRegistration = d.StudentRegistration,
                DocumentType = d.DocumentType,
                Status = d.Status,
                CurrentVersion = d.CurrentVersion,
                CreatedAtUtc = d.CreatedAtUtc,
                CurrentFileHash = latestVersion?.FileHashSha256 ?? string.Empty,
                CreatedBy = d.CreatedBy,
                Versions = d.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new DocumentVersionDto
                {
                    VersionNumber = v.VersionNumber,
                    FileHash = v.FileHashSha256,
                    FileSizeBytes = v.FileSize,
                    CreatedAtUtc = v.CreatedAtUtc,
                    UploadedBy = v.CreatedByUserId,
                    MimeType = v.MimeType,
                    StoragePath = v.StoragePath
                }).ToList()
            };
        }).ToList();

        return new PagedResult<DocumentResponseDto>(
            dtos, 
            totalCount, 
            pageNumber, 
            pageSize, 
            "Búsqueda paginada completada exitosamente.");
    }

    public async Task<Result<DocumentDownloadDto>> DownloadDocumentAsync(
        Guid documentId, 
        int? versionNumber = null, 
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetWithDetailsAsync(documentId, cancellationToken);
        if (document is null)
        {
            return Result<DocumentDownloadDto>.Failure("El documento especificado no existe.", "DOCUMENT_NOT_FOUND");
        }

        var version = versionNumber.HasValue
            ? document.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber.Value)
            : document.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (version is null)
        {
            return Result<DocumentDownloadDto>.Failure("La versión solicitada del documento no existe.", "VERSION_NOT_FOUND");
        }

        try
        {
            var stream = await _storageService.GetFileStreamAsync(version.StoragePath, cancellationToken);

            // Record audit log for download
            var currentUserId = string.IsNullOrWhiteSpace(_currentUserService.UserId) ? "System" : _currentUserService.UserId;
            var auditLog = new AuditLog(
                currentUserId,
                AuditAction.Downloaded,
                document.Id,
                _currentUserService.IpAddress,
                $"DOCUMENT_DOWNLOADED: Versión {version.VersionNumber} descargada.");

            await _unitOfWork.Repository<AuditLog, Guid>().AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Extract user-friendly filename from storage key
            var rawFileName = Path.GetFileName(version.StoragePath);
            var friendlyFileName = rawFileName;
            if (friendlyFileName.StartsWith($"v{version.VersionNumber}_", StringComparison.OrdinalIgnoreCase))
            {
                friendlyFileName = friendlyFileName.Substring($"v{version.VersionNumber}_".Length);
            }
            if (!string.IsNullOrEmpty(version.FileHashSha256) && friendlyFileName.StartsWith($"{version.FileHashSha256}_", StringComparison.OrdinalIgnoreCase))
            {
                friendlyFileName = friendlyFileName.Substring(version.FileHashSha256.Length + 1);
            }
            else
            {
                var underscoreIndex = friendlyFileName.IndexOf('_');
                if (underscoreIndex >= 0 && underscoreIndex + 1 < friendlyFileName.Length)
                {
                    friendlyFileName = friendlyFileName.Substring(underscoreIndex + 1);
                }
            }

            var downloadDto = new DocumentDownloadDto
            {
                Content = stream,
                FileName = friendlyFileName,
                ContentType = string.IsNullOrWhiteSpace(version.MimeType) ? "application/octet-stream" : version.MimeType,
                FileSize = version.FileSize,
                VersionNumber = version.VersionNumber
            };

            return Result<DocumentDownloadDto>.Success(downloadDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document '{DocumentId}' version '{Version}'", documentId, version.VersionNumber);
            return Result<DocumentDownloadDto>.Failure("Error al recuperar el archivo desde el almacenamiento.", "STORAGE_DOWNLOAD_ERROR");
        }
    }

    public async Task<Result<IReadOnlyList<DocumentVersionDto>>> GetVersionsAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetWithDetailsAsync(documentId, cancellationToken);
        if (document is null)
        {
            return Result<IReadOnlyList<DocumentVersionDto>>.Failure(
                $"El documento con ID '{documentId}' no fue encontrado.",
                "DOCUMENT_NOT_FOUND");
        }

        var versionDtos = document.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionDto
            {
                VersionNumber = v.VersionNumber,
                FileHash = v.FileHashSha256,
                FileSizeBytes = v.FileSize,
                CreatedAtUtc = v.CreatedAtUtc,
                UploadedBy = v.CreatedByUserId,
                MimeType = v.MimeType,
                StoragePath = v.StoragePath
            }).ToList();

        return Result<IReadOnlyList<DocumentVersionDto>>.Success(
            versionDtos,
            $"Historial de versiones recuperado exitosamente ({versionDtos.Count} versiones encontradas).");
    }
}
