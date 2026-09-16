namespace LexisCampusDMS.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Role { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
