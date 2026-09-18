using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace LexisCampusDMS.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Downloads the binary content of a document (optionally for a specific version).
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid id, 
        [FromQuery] int? versionNumber, 
        CancellationToken cancellationToken)
    {
        var result = await _documentService.DownloadDocumentAsync(id, versionNumber, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(result);
        }

        return File(result.Data!, "application/octet-stream", enableRangeProcessing: true);
    }
}
