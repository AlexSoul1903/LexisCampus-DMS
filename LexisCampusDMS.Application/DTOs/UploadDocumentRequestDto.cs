using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Application.DTOs;

public record UploadDocumentRequestDto
{
    public string Title { get; init; } = string.Empty;
    public string StudentRegistration { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string? InitialComment { get; init; }
    public Stream FileStream { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
}
