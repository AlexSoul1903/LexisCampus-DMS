using LexisCampusDMS.Application.Common;
using LexisCampusDMS.Application.DTOs;

namespace LexisCampusDMS.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<PagedResult<UserResponseDto>> GetUsersAsync(UserFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> ChangeUserRoleAsync(Guid id, ChangeUserRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<UserResponseDto>> ChangeUserStatusAsync(Guid id, ChangeUserStatusRequestDto request, CancellationToken cancellationToken = default);
}
