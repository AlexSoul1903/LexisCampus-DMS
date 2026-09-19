namespace LexisCampusDMS.Application.DTOs;

public record UserResponseDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? StudentRegistration { get; init; }
    public bool IsActive { get; init; }
    public bool IsLockedOut { get; init; }
    public DateTime? LockoutEndUtc { get; init; }
    public int FailedLoginAttempts { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? LastModifiedAtUtc { get; init; }
    public string? LastModifiedBy { get; init; }
}
