using System.Text;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Services;
using LexisCampusDMS.Application.Validators;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LexisCampusDMS.UnitTests.Application;

public class DocumentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDocumentRepository> _documentRepoMock = new();
    private readonly Mock<IStorageService> _storageServiceMock = new();
    private readonly Mock<IHashService> _hashServiceMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IGenericRepository<AuditLog, Guid>> _auditRepoMock = new();
    private readonly UploadDocumentRequestDtoValidator _validator = new();
    private readonly RectifyDocumentRequestDtoValidator _rectifyValidator = new();

    private readonly DocumentService _documentService;

    public DocumentServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<AuditLog, Guid>()).Returns(_auditRepoMock.Object);

        _currentUserMock.Setup(u => u.UserId).Returns("usr-12345");
        _currentUserMock.Setup(u => u.IpAddress).Returns("192.168.1.100");

        _documentService = new DocumentService(
            _unitOfWorkMock.Object,
            _documentRepoMock.Object,
            _storageServiceMock.Object,
            _hashServiceMock.Object,
            _currentUserMock.Object,
            _validator,
            _rectifyValidator,
            NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task UploadDocumentAsync_ValidRequest_CalculatesHashUploadsToMinioAndPersistsTransactionally()
    {
        // Arrange
        var content = "PDF Document Mock Content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var expectedHash = "a1b2c3d4e5f678901234567890abcdef1234567890abcdef1234567890abcdef";

        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHash);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("2023-0145/2026/test_Certificado.pdf");

        var request = new UploadDocumentRequestDto
        {
            Title = "Certificado de Estudios 2026",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.StudyCertificate,
            FileName = "Certificado.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert: 1. Success result
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Certificado de Estudios 2026", result.Data.Title);
        Assert.Equal("2023-0145", result.Data.StudentRegistration);
        Assert.Equal(1, result.Data.CurrentVersion);
        Assert.Equal(expectedHash, result.Data.CurrentFileHash);

        // Assert: 2. Storage upload was called
        _storageServiceMock.Verify(s => s.UploadFileAsync(
            stream, 
            It.Is<string>(path => path.Contains("2023-0145") && path.Contains(expectedHash)), 
            "application/pdf", 
            It.IsAny<CancellationToken>()), Times.Once);

        // Assert: 3. DB Transaction and Document registration
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _documentRepoMock.Verify(d => d.RegisterDocumentAsync(
            It.Is<Document>(doc => doc.Title == request.Title && doc.StudentRegistration == "2023-0145"), 
            It.IsAny<CancellationToken>()), Times.Once);

        // Assert: 4. AuditLog registration with DOCUMENT_UPLOADED action
        _auditRepoMock.Verify(a => a.AddAsync(
            It.Is<AuditLog>(log => log.Action == AuditAction.DocumentUploaded && log.UserId == "usr-12345"), 
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_InvalidMetadata_ReturnsFailureWithoutCallingStorageOrDb()
    {
        // Arrange: Missing title and empty file
        var request = new UploadDocumentRequestDto
        {
            Title = "", // Invalid
            StudentRegistration = "", // Invalid
            FileName = "",
            FileStream = Stream.Null,
            FileSizeBytes = 0
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.NotEmpty(result.Errors);

        _hashServiceMock.Verify(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_DatabaseFailure_RollsBackAndDeletesStorageBlob()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test Stream"));
        var expectedHash = "hash12345";
        var expectedPath = "2023-0145/2026/hash12345_Record.pdf";

        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHash);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPath);

        // Force DB commit failure
        _unitOfWorkMock
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated Database Timeout / Deadlock"));

        var request = new UploadDocumentRequestDto
        {
            Title = "Record de Notas",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.Transcript,
            FileName = "Record.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PERSISTENCE_TRANSACTION_FAILED", result.ErrorCode);

        // Transaction rollback must have been invoked
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Compensation: Storage delete must have been called to prevent orphan blob in MinIO!
        _storageServiceMock.Verify(s => s.DeleteFileAsync(expectedPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_DisallowedExtension_ReturnsValidationError()
    {
        // Arrange: Executable file extension
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("echo dangerous script"));
        var request = new UploadDocumentRequestDto
        {
            Title = "Script Malicioso",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.Other,
            FileName = "payload.exe",
            ContentType = "application/x-msdownload",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains(result.Errors, e => e.Contains(".pdf, .png, .jpg y .jpeg"));

        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadDocumentAsync_FileSizeExceedsLimit_ReturnsValidationError()
    {
        // Arrange: File size exceeding 50 MB
        using var stream = new MemoryStream(new byte[10]);
        var request = new UploadDocumentRequestDto
        {
            Title = "Documento Gigante",
            StudentRegistration = "2023-0145",
            DocumentType = DocumentType.Other,
            FileName = "large.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = (50L * 1024 * 1024) + 1 // 50MB + 1 byte
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains(result.Errors, e => e.Contains("50 MB"));

        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RectifyDocumentAsync_ValidRequestById_IncrementsVersionUploadsToMinioAndAudits()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var existingDoc = new Document("Certificado de Notas", "2023-0145", DocumentType.Transcript, "usr-original");
        existingDoc.AddNewVersion("2023-0145/2026/v1_oldhash_Certificado.pdf", "oldhash123", 1024, "application/pdf", "usr-original");

        _documentRepoMock
            .Setup(r => r.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDoc);

        var newHash = "b2c3d4e5f678901234567890abcdef1234567890abcdef1234567890abcdef12";
        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newHash);

        var expectedStoragePath = $"2023-0145/{DateTime.UtcNow.Year}/v2_{newHash}_Certificado_Rectificado.pdf";
        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStoragePath);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Rectified PDF Content"));
        var request = new RectifyDocumentRequestDto
        {
            DocumentId = docId,
            ChangeReason = "Corrección de calificación de asignatura Cálculo II",
            FileName = "Certificado_Rectificado.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.RectifyDocumentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.CurrentVersion);
        Assert.Equal(newHash, result.Data!.CurrentFileHash);
        Assert.NotNull(result.Data!.Versions);
        Assert.Equal(2, result.Data!.Versions!.Count);

        // Verify storage upload called
        _storageServiceMock.Verify(s => s.UploadFileAsync(
            stream,
            It.Is<string>(p => p.Contains("2023-0145") && p.Contains("v2") && p.Contains(newHash)),
            "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify transactional persistence
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify AuditLog contains DocumentRectified and the change reason
        _auditRepoMock.Verify(a => a.AddAsync(
            It.Is<AuditLog>(l => l.Action == AuditAction.DocumentRectified &&
                                 l.Details!.Contains("Motivo: Corrección de calificación") &&
                                 l.Details.Contains("Versión 2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RectifyDocumentAsync_ValidRequestByStudentAndType_FindsDocAndAddsVersion()
    {
        // Arrange
        var existingDoc = new Document("Título de Grado", "2024-5555", DocumentType.Degree, "usr-original");
        existingDoc.AddNewVersion("2024-5555/2026/v1_old_Grado.pdf", "oldhash", 2048, "application/pdf", "usr-original");

        _documentRepoMock
            .Setup(r => r.GetByRegistrationAndTypeWithDetailsAsync("2024-5555", DocumentType.Degree, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDoc);

        var newHash = "newhash456";
        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newHash);

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path/to/v2");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Rectified degree"));
        var request = new RectifyDocumentRequestDto
        {
            StudentRegistration = "2024-5555",
            DocumentType = DocumentType.Degree,
            ChangeReason = "Actualización de firma del rector",
            FileName = "Titulo_Firmado.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.RectifyDocumentAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.CurrentVersion);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RectifyDocumentAsync_DocumentNotFound_ReturnsFailureWithoutCallingStorage()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _documentRepoMock
            .Setup(r => r.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("PDF content"));
        var request = new RectifyDocumentRequestDto
        {
            DocumentId = docId,
            ChangeReason = "Rectificación de documento inexistente",
            FileName = "Rect.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.RectifyDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("DOCUMENT_NOT_FOUND", result.ErrorCode);

        _hashServiceMock.Verify(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RectifyDocumentAsync_EmptyChangeReason_ReturnsValidationError()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("PDF content"));
        var request = new RectifyDocumentRequestDto
        {
            DocumentId = Guid.NewGuid(),
            ChangeReason = "", // Empty reason should fail validation
            FileName = "Rect.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.RectifyDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains(result.Errors, e => e.Contains("motivo"));

        _documentRepoMock.Verify(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RectifyDocumentAsync_DisallowedExtension_ReturnsValidationError()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("dangerous code"));
        var request = new RectifyDocumentRequestDto
        {
            DocumentId = Guid.NewGuid(),
            ChangeReason = "Actualización con script no autorizado",
            FileName = "malicious_script.bat",
            ContentType = "application/x-bat",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.RectifyDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains(result.Errors, e => e.Contains(".pdf, .png, .jpg y .jpeg"));

        _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RectifyDocumentAsync_DatabaseFailure_RollsBackAndCompensatesStorage()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var existingDoc = new Document("Título", "2023-0145", DocumentType.StudyCertificate, "usr-original");
        existingDoc.AddNewVersion("path/v1", "hash1", 100, "application/pdf", "usr-original");

        _documentRepoMock
            .Setup(r => r.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDoc);

        var uploadedPath = "2023-0145/2026/v2_hash2_file.pdf";
        _hashServiceMock
            .Setup(h => h.ComputeSha256Async(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("hash2");

        _storageServiceMock
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedPath);

        _unitOfWorkMock
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated SQL deadlock"));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Rect content"));
        var request = new RectifyDocumentRequestDto
        {
            DocumentId = docId,
            ChangeReason = "Rectificación legítima con error de persistencia simulado",
            FileName = "file.pdf",
            ContentType = "application/pdf",
            FileStream = stream,
            FileSizeBytes = stream.Length
        };

        // Act
        var result = await _documentService.RectifyDocumentAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PERSISTENCE_TRANSACTION_FAILED", result.ErrorCode);

        // Verify transaction rollback
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify compensation: deleted uploaded MinIO object
        _storageServiceMock.Verify(s => s.DeleteFileAsync(uploadedPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ValidFilter_ReturnsPagedResult()
    {
        // Arrange
        var doc1 = new Document("Doc 1", "2023-0001", DocumentType.Transcript, "usr");
        doc1.AddNewVersion("p1", "h1", 100, "application/pdf", "usr");
        var doc2 = new Document("Doc 2", "2023-0002", DocumentType.Transcript, "usr");
        doc2.AddNewVersion("p2", "h2", 200, "application/pdf", "usr");

        var pagedItems = (IReadOnlyList<Document>)new List<Document> { doc1, doc2 };
        _documentRepoMock
            .Setup(r => r.SearchAsync(
                "2023", 
                DocumentType.Transcript, 
                null, 
                null, 
                1, 
                10, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((pagedItems, 25));

        var filter = new SearchFilterDto
        {
            StudentRegistration = "2023",
            DocumentType = DocumentType.Transcript,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _documentService.SearchAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(2, result.Data!.Count);
    }

    [Fact]
    public async Task SearchAsync_InvalidPagination_CorrectsToDefaults()
    {
        // Arrange
        _documentRepoMock
            .Setup(r => r.SearchAsync(
                null, 
                null, 
                null, 
                null, 
                1, 
                10, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Document>)new List<Document>(), 0));

        var filter = new SearchFilterDto
        {
            PageNumber = -5, // Invalid, should default to 1
            PageSize = 0     // Invalid, should default to 10
        };

        // Act
        var result = await _documentService.SearchAsync(filter);

        // Assert
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        _documentRepoMock.Verify(r => r.SearchAsync(null, null, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithValidDocument_ReturnsDocumentDownloadDto()
    {
        // Arrange
        var doc = new Document("Expediente", "2023-0145", DocumentType.Degree, "usr");
        var docId = doc.Id;
        doc.AddNewVersion("2023-0145/2026/v1_hash999_Titulo.pdf", "hash999", 5000, "application/pdf", "usr");

        _documentRepoMock
            .Setup(r => r.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var mockStream = new MemoryStream(Encoding.UTF8.GetBytes("Binary PDF"));
        _storageServiceMock
            .Setup(s => s.GetFileStreamAsync("2023-0145/2026/v1_hash999_Titulo.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStream);

        // Act
        var result = await _documentService.DownloadDocumentAsync(docId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Titulo.pdf", result.Data.FileName);
        Assert.Equal("application/pdf", result.Data.ContentType);
        Assert.Equal(1, result.Data.VersionNumber);
        Assert.NotNull(result.Data.Content);

        // Audit log must be recorded
        _auditRepoMock.Verify(a => a.AddAsync(
            It.Is<AuditLog>(l => l.Action == AuditAction.Downloaded && l.DocumentId == docId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVersionsAsync_ExistingDocument_ReturnsOrderedVersionsList()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var doc = new Document("Documento Histórico", "2023-0145", DocumentType.Transcript, "usr");
        doc.AddNewVersion("p1", "h1", 100, "application/pdf", "usr");
        doc.AddNewVersion("p2", "h2", 200, "application/pdf", "usr");

        _documentRepoMock
            .Setup(r => r.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        // Act
        var result = await _documentService.GetVersionsAsync(docId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.Data[0].VersionNumber);
        Assert.Equal(1, result.Data[1].VersionNumber);
    }

    [Fact]
    public async Task GetVersionsAsync_DocumentNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var docId = Guid.NewGuid();
        _documentRepoMock
            .Setup(r => r.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.GetVersionsAsync(docId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("DOCUMENT_NOT_FOUND", result.ErrorCode);
    }
}
