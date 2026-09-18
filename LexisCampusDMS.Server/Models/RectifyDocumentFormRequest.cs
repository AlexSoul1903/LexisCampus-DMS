using System.ComponentModel.DataAnnotations;
using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Server.Models;

public record RectifyDocumentFormRequest
{
    [Required]
    public IFormFile File { get; init; } = default!;

    [Required]
    [MaxLength(500)]
    public string ChangeReason { get; init; } = string.Empty;

    [MaxLength(50)]
    public string? StudentRegistration { get; init; }

    public DocumentType? DocumentType { get; init; }
}
