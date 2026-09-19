namespace LexisCampusDMS.Application.DTOs;

public record CreateUserRequestDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? StudentRegistration { get; init; }
}
