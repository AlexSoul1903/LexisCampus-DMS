using LexisCampusDMS.Core.Domain.Entities;

namespace LexisCampusDMS.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null);
}
