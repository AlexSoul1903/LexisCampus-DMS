namespace LexisCampusDMS.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Role { get; }
    IReadOnlyList<string> Roles { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
