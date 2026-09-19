namespace LexisCampusDMS.Application.DTOs;

public class StudentDossierManifestDto
{
    public string StudentRegistration { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public List<StudentDossierManifestItemDto> Documents { get; set; } = new();
}

public class StudentDossierManifestItemDto
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileHashSha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime IssueDateUtc { get; set; }
}
