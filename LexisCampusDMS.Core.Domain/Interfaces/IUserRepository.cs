using LexisCampusDMS.Core.Domain.Entities;

namespace LexisCampusDMS.Core.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User, Guid>
{
    Task<User?> GetByUsernameOrEmailWithTokensAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
