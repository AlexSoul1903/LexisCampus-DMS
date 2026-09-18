using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Enums;
using LexisCampusDMS.Core.Domain.Interfaces;
using LexisCampusDMS.Infraestructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LexisCampusDMS.Infraestructure.Persistence.Repositories;

public class DocumentRepository : GenericRepository<Document, Guid>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Document> RegisterDocumentAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document, nameof(document));
        await _dbSet.AddAsync(document, cancellationToken);
        return document;
    }

    public async Task<Document?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
            .Include(d => d.AuditLogs.OrderByDescending(a => a.TimestampUtc))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByStudentRegistrationAsync(
        string studentRegistration, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentRegistration, nameof(studentRegistration));

        return await _dbSet
            .AsNoTracking()
            .Include(d => d.Versions)
            .Where(d => d.StudentRegistration == studentRegistration.Trim())
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentVersion>> GetVersionsAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }
}
