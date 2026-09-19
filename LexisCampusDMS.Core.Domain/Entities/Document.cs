using LexisCampusDMS.Core.Domain.Common;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Exceptions;

namespace LexisCampusDMS.Core.Domain.Entities;

public class Document : AuditableEntity<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string StudentRegistration { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public int CurrentVersion { get; set; } = 1;

    // Legal Revocation & Annulment fields
    public string? ResolutionNumber { get; set; }
    public string? RevocationReason { get; set; }
    public string? RevocationObservations { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedBy { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Parameterless constructor for ORM / deserialization
    public Document()
    {
        Id = Guid.NewGuid();
    }

    public Document(
        string title,
        string studentRegistration,
        DocumentType documentType,
        string createdBy) : this()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException(nameof(title), "El título del documento no puede estar vacío.");

        if (string.IsNullOrWhiteSpace(studentRegistration))
            throw new DomainValidationException(nameof(studentRegistration), "La matrícula del estudiante no puede estar vacía.");

        Title = title.Trim();
        StudentRegistration = studentRegistration.Trim();
        DocumentType = documentType;
        Status = DocumentStatus.Draft;
        CurrentVersion = 1;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public DocumentVersion AddNewVersion(
        string storagePath,
        string fileHashSha256,
        long fileSize,
        string mimeType,
        string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new DomainValidationException(nameof(storagePath), "La ruta de almacenamiento es requerida para una nueva versión.");

        if (string.IsNullOrWhiteSpace(fileHashSha256))
            throw new DomainValidationException(nameof(fileHashSha256), "El hash SHA-256 del archivo es requerido para garantizar su integridad.");

        if (fileSize <= 0)
            throw new DomainValidationException(nameof(fileSize), "El tamaño del archivo debe ser mayor a cero.");

        var nextVersionNumber = (Versions.Count > 0 ? Versions.Max(v => v.VersionNumber) : 0) + 1;
        CurrentVersion = nextVersionNumber;

        var version = new DocumentVersion(
            Id,
            nextVersionNumber,
            storagePath,
            fileHashSha256,
            fileSize,
            mimeType,
            createdByUserId);

        Versions.Add(version);
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = createdByUserId;

        return version;
    }

    public void UpdateTitle(string newTitle, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainValidationException(nameof(newTitle), "El título del documento no puede estar vacío.");

        Title = newTitle.Trim();
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void ChangeStatus(DocumentStatus newStatus, string modifiedBy)
    {
        if (Status == DocumentStatus.Archived && newStatus != DocumentStatus.Archived)
            throw new InvalidDocumentStateException(Status, newStatus);

        if (Status == DocumentStatus.Rejected && newStatus == DocumentStatus.Approved)
            throw new InvalidDocumentStateException(Status, newStatus);

        Status = newStatus;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Revoke(string resolutionNumber, string reason, string? observations, string revokedBy)
    {
        if (Status == DocumentStatus.Revoked)
        {
            throw new DomainValidationException(nameof(Status), "El documento ya se encuentra anulado/revocado.");
        }

        if (string.IsNullOrWhiteSpace(resolutionNumber))
        {
            throw new DomainValidationException(nameof(resolutionNumber), "El número de resolución es obligatorio para anular el documento.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException(nameof(reason), "El motivo legal de revocación es obligatorio.");
        }

        Status = DocumentStatus.Revoked;
        ResolutionNumber = resolutionNumber.Trim();
        RevocationReason = reason.Trim();
        RevocationObservations = observations?.Trim();
        RevokedAtUtc = DateTime.UtcNow;
        RevokedBy = revokedBy;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = revokedBy;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = deletedBy;
    }
}
