namespace LexisCampusDMS.Application.DTOs;

public record UpdateUserRequestDto
{
    public string FullName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? StudentRegistration { get; init; }
}
