using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexisCampusDMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Registro,Admin,Auditor")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a new document and registers its initial version (v1) in storage and database.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Registro,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentFormRequest formRequest, 
        CancellationToken cancellationToken)
    {
        if (formRequest.File is null || formRequest.File.Length == 0)
        {
            return BadRequest(Result<DocumentResponseDto>.Failure(
                "Debe proporcionar un archivo válido para la carga.",
                "INVALID_FILE"));
        }

        await using var stream = formRequest.File.OpenReadStream();

        var requestDto = new UploadDocumentRequestDto
        {
            Title = formRequest.Title,
            StudentRegistration = formRequest.StudentRegistration,
            DocumentType = formRequest.DocumentType,
            InitialComment = formRequest.InitialComment,
            FileStream = stream,
            FileName = formRequest.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(formRequest.File.ContentType) 
                ? "application/octet-stream" 
                : formRequest.File.ContentType,
            FileSizeBytes = formRequest.File.Length
        };

        var result = await _documentService.UploadDocumentAsync(requestDto, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetById), 
            new { id = result.Data!.Id }, 
            result);
    }

    /// <summary>
    /// Uploads a rectified file version for an existing document by its ID.
    /// </summary>
    [HttpPost("{id:guid}/rectify")]
    [Authorize(Roles = "Registro,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RectifyById(
        Guid id,
        [FromForm] RectifyDocumentFormRequest formRequest,
        CancellationToken cancellationToken)
    {
        if (formRequest.File is null || formRequest.File.Length == 0)
        {
            return BadRequest(Result<DocumentResponseDto>.Failure(
                "Debe proporcionar un archivo válido para la rectificación.",
                "INVALID_FILE"));
        }

        await using var stream = formRequest.File.OpenReadStream();

        var requestDto = new RectifyDocumentRequestDto
        {
            DocumentId = id,
            ChangeReason = formRequest.ChangeReason,
            FileStream = stream,
            FileName = formRequest.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(formRequest.File.ContentType)
                ? "application/octet-stream"
                : formRequest.File.ContentType,
            FileSizeBytes = formRequest.File.Length
        };

        var result = await _documentService.RectifyDocumentAsync(requestDto, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "DOCUMENT_NOT_FOUND" ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Uploads a rectified file version for an existing document by student registration and document type.
    /// </summary>
    [HttpPost("rectify")]
    [Authorize(Roles = "Registro,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rectify(
        [FromForm] RectifyDocumentFormRequest formRequest,
        CancellationToken cancellationToken)
    {
        if (formRequest.File is null || formRequest.File.Length == 0)
        {
            return BadRequest(Result<DocumentResponseDto>.Failure(
                "Debe proporcionar un archivo válido para la rectificación.",
                "INVALID_FILE"));
        }

        await using var stream = formRequest.File.OpenReadStream();

        var requestDto = new RectifyDocumentRequestDto
        {
            StudentRegistration = formRequest.StudentRegistration,
            DocumentType = formRequest.DocumentType,
            ChangeReason = formRequest.ChangeReason,
            FileStream = stream,
            FileName = formRequest.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(formRequest.File.ContentType)
                ? "application/octet-stream"
                : formRequest.File.ContentType,
            FileSizeBytes = formRequest.File.Length
        };

        var result = await _documentService.RectifyDocumentAsync(requestDto, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == "DOCUMENT_NOT_FOUND" ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieves document metadata and version history by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieves all documents registered for a specific student registration.
    /// </summary>
    [HttpGet("student/{studentRegistration}")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<DocumentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<DocumentResponseDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStudent(string studentRegistration, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetByStudentRegistrationAsync(studentRegistration, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Searches and filters documents with pagination by student registration, document type, and date range.
    /// </summary>
    /// <param name="matricula">Student registration number (Spanish parameter alias).</param>
    /// <param name="studentRegistration">Student registration number.</param>
    /// <param name="tipo">Document type enum (Spanish parameter alias).</param>
    /// <param name="documentType">Document type enum.</param>
    /// <param name="fromDateUtc">Filter documents created on or after this UTC date.</param>
    /// <param name="toDateUtc">Filter documents created on or before this UTC date.</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing documents matching criteria.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<DocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery(Name = "matricula")] string? matricula,
        [FromQuery(Name = "studentRegistration")] string? studentRegistration,
        [FromQuery(Name = "tipo")] DocumentType? tipo,
        [FromQuery(Name = "documentType")] DocumentType? documentType,
        [FromQuery] DateTime? fromDateUtc,
        [FromQuery] DateTime? toDateUtc,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var effectiveRegistration = !string.IsNullOrWhiteSpace(matricula) ? matricula : studentRegistration;
        var effectiveType = tipo ?? documentType;

        var filter = new SearchFilterDto
        {
            StudentRegistration = effectiveRegistration,
            DocumentType = effectiveType,
            FromDateUtc = fromDateUtc,
            ToDateUtc = toDateUtc,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _documentService.SearchAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the complete version history, metadata, and cryptographic hashes for a document.
    /// </summary>
    /// <param name="id">Document unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<DocumentVersionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<DocumentVersionDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.GetVersionsAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Downloads the binary content of a document (optionally for a specific version).
    /// </summary>
    /// <param name="id">Document unique identifier.</param>
    /// <param name="versionNumber">Optional specific version number (defaults to latest version).</param>
    /// <param name="inline">True to display inline in browser; false (default) for attachment download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid id, 
        [FromQuery] int? versionNumber, 
        [FromQuery] bool inline = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _documentService.DownloadDocumentAsync(id, versionNumber, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        var fileName = string.IsNullOrWhiteSpace(result.Data!.FileName) ? "document" : result.Data.FileName;
        var contentDisposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue(inline ? "inline" : "attachment");
        contentDisposition.SetHttpFileName(fileName);
        Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return File(result.Data.Content, result.Data.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Legally revokes and annuls an existing document with mandatory resolution number and legal reason.
    /// Preserves binary files in storage and historical forensic audit trail.
    /// </summary>
    /// <param name="id">Document unique identifier.</param>
    /// <param name="request">Revocation metadata (Reason, ResolutionNumber, Observations).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Document successfully revoked and marked as Revoked.</response>
    /// <response code="400">Validation error or invalid input parameters.</response>
    /// <response code="404">Document not found.</response>
    [HttpPost("{id:guid}/revoke")]
    [Authorize(Roles = "Admin,Registro")]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DocumentResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid id, 
        [FromBody] RevokeDocumentDto request, 
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Result<DocumentResponseDto>.Failure(
                "Debe proporcionar los datos legales para la revocación.",
                "INVALID_INPUT"));
        }

        var result = await _documentService.RevokeDocumentAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "DOCUMENT_NOT_FOUND" ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Public verification endpoint that certifies document authenticity or emits a legal revocation warning seal.
    /// Accessible without authentication for external institutional verifications (e.g. via QR code).
    /// </summary>
    /// <param name="id">Document unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Document verification status retrieved.</response>
    /// <response code="404">Document not found in institutional records.</response>
    [HttpGet("{id:guid}/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<DocumentVerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DocumentVerificationResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(Guid id, CancellationToken cancellationToken)
    {
        var result = await _documentService.VerifyDocumentAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Downloads all active (non-revoked) documents for a student in a single ZIP archive,
    /// including an audit manifest file (resumen_expediente.json) with SHA-256 hashes and metadata.
    /// Memory usage is optimized with RecyclableMemoryStream to prevent LOH saturation.
    /// </summary>
    /// <param name="matricula">Student registration number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">ZIP archive containing all active documents and manifest.</response>
    /// <response code="400">Invalid or empty student registration number.</response>
    /// <response code="404">No active documents found for the specified student.</response>
    [HttpGet("student/{matricula}/dossier-zip")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK, "application/zip")]
    [ProducesResponseType(typeof(Result<StudentDossierDownloadDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<StudentDossierDownloadDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadStudentDossierZip(
        string matricula, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(matricula))
        {
            return BadRequest(Result<StudentDossierDownloadDto>.Failure(
                "La matrícula del estudiante es requerida.",
                "INVALID_STUDENT_REGISTRATION"));
        }

        var result = await _documentService.DownloadStudentDossierZipAsync(matricula, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "STUDENT_DOSSIER_NOT_FOUND" 
                ? NotFound(result) 
                : BadRequest(result);
        }

        return File(result.Data!.Stream, result.Data.ContentType, result.Data.FileName);
    }
}
