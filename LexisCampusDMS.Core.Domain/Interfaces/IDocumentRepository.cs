using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Core.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document, Guid>
{
    Task<Document> RegisterDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Document?> GetByRegistrationAndTypeWithDetailsAsync(string studentRegistration, DocumentType documentType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByStudentRegistrationAsync(string studentRegistration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Document> Items, int TotalCount)> SearchAsync(
        string? studentRegistration,
        DocumentType? documentType,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<DocumentVersion?> GetVersionByHashAsync(string fileHashSha256, CancellationToken cancellationToken = default);
}
