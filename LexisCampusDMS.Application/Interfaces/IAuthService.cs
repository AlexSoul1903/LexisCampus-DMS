using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
