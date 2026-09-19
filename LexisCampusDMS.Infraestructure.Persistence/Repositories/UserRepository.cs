using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Core.Domain.Interfaces;
using LexisCampusDMS.Infraestructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LexisCampusDMS.Infraestructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User, Guid>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByUsernameOrEmailWithTokensAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var normalized = usernameOrEmail.Trim();

        return await _dbSet
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => (u.Username == normalized || u.Email == normalized) && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<RefreshToken>()
            .Include(r => r.User)
            .Where(r => r.Token == refreshToken)
            .Select(r => r.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> SearchUsersAsync(
        string? searchTerm,
        string? role,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = _dbSet.AsNoTracking().Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(u => u.Username.Contains(term) ||
                                     u.Email.Contains(term) ||
                                     u.FullName.Contains(term) ||
                                     (u.StudentRegistration != null && u.StudentRegistration.Contains(term)) ||
                                     (u.Department != null && u.Department.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleTrimmed = role.Trim();
            query = query.Where(u => u.Role == roleTrimmed);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
