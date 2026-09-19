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
}
