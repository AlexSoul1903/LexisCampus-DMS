namespace LexisCampusDMS.Application.DTOs;

public record DocumentDownloadDto
{
    public Stream Content { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long FileSize { get; init; }
    public int VersionNumber { get; init; }
}
