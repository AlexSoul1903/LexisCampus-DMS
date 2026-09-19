namespace LexisCampusDMS.Application.DTOs;

public class StudentDossierDownloadDto
{
    public Stream Stream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/zip";
}
