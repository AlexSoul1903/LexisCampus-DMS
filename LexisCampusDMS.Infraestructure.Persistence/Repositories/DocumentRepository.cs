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

    public async Task<Document?> GetByRegistrationAndTypeWithDetailsAsync(
        string studentRegistration, 
        DocumentType documentType, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentRegistration, nameof(studentRegistration));

        return await _dbSet
            .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
            .Include(d => d.AuditLogs.OrderByDescending(a => a.TimestampUtc))
            .FirstOrDefaultAsync(d => d.StudentRegistration == studentRegistration.Trim() && d.DocumentType == documentType, cancellationToken);
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

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> SearchAsync(
        string? studentRegistration,
        DocumentType? documentType,
        DateTime? fromDateUtc,
        DateTime? toDateUtc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = _dbSet
            .AsNoTracking()
            .Include(d => d.Versions);

        if (!string.IsNullOrWhiteSpace(studentRegistration))
        {
            var reg = studentRegistration.Trim();
            query = query.Where(d => d.StudentRegistration.Contains(reg));
        }

        if (documentType.HasValue)
        {
            query = query.Where(d => d.DocumentType == documentType.Value);
        }

        if (fromDateUtc.HasValue)
        {
            query = query.Where(d => d.CreatedAtUtc >= fromDateUtc.Value);
        }

        if (toDateUtc.HasValue)
        {
            query = query.Where(d => d.CreatedAtUtc <= toDateUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<DocumentVersion?> GetVersionByHashAsync(
        string fileHashSha256, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHashSha256, nameof(fileHashSha256));

        var normalizedHash = fileHashSha256.Trim().ToLowerInvariant();

        return await _dbContext.DocumentVersions
            .AsNoTracking()
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.FileHashSha256.ToLower() == normalizedHash, cancellationToken);
    }
}
