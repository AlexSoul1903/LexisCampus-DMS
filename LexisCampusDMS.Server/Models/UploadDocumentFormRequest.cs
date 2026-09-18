using System.ComponentModel.DataAnnotations;
using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Server.Models;

public record UploadDocumentFormRequest
{
    [Required]
    public IFormFile File { get; init; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string StudentRegistration { get; init; } = string.Empty;

    [Required]
    public DocumentType DocumentType { get; init; }

    [MaxLength(500)]
    public string? InitialComment { get; init; }
}
