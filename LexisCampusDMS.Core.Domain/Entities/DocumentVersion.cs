using LexisCampusDMS.Core.Domain.Common;

namespace LexisCampusDMS.Core.Domain.Entities;

public class DocumentVersion : BaseEntity<Guid>
{
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public int VersionNumber { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string FileHashSha256 { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;

    // Parameterless constructor for ORM / deserialization
    public DocumentVersion()
    {
        Id = Guid.NewGuid();
    }

    public DocumentVersion(
        Guid documentId,
        int versionNumber,
        string storagePath,
        string fileHashSha256,
        long fileSize,
        string mimeType,
        string createdByUserId) : this()
    {
        DocumentId = documentId;
        VersionNumber = versionNumber;
        StoragePath = storagePath;
        FileHashSha256 = fileHashSha256;
        FileSize = fileSize;
        MimeType = mimeType;
        CreatedByUserId = createdByUserId;
    }
}
