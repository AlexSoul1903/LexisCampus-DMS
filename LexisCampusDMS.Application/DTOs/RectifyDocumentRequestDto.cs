using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Application.DTOs;

public record RectifyDocumentRequestDto
{
    public Guid? DocumentId { get; init; }
    public string? StudentRegistration { get; init; }
    public DocumentType? DocumentType { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public Stream FileStream { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
}
