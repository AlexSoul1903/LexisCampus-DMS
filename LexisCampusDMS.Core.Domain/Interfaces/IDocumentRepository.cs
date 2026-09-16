using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Core.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document, Guid>
{
    Task<Document> RegisterDocumentAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByStudentRegistrationAsync(string studentRegistration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken cancellationToken = default);
}
