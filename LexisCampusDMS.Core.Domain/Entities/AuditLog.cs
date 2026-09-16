using LexisCampusDMS.Core.Domain.Common;
using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Core.Domain.Entities;

public class AuditLog : BaseEntity<Guid>
{
    public string UserId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? Details { get; set; }

    // Parameterless constructor for ORM / deserialization
    public AuditLog()
    {
        Id = Guid.NewGuid();
    }

    public AuditLog(
        string userId,
        AuditAction action,
        Guid? documentId = null,
        string? ipAddress = null,
        string? details = null) : this()
    {
        UserId = userId;
        Action = action;
        DocumentId = documentId;
        IpAddress = ipAddress;
        Details = details;
        TimestampUtc = DateTime.UtcNow;
    }
}
