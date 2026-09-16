namespace LexisCampusDMS.Application.DTOs;

public record DocumentVersionDto
{
    public int VersionNumber { get; init; }
    public string FileHash { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string UploadedBy { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
}
