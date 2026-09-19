namespace LexisCampusDMS.Application.DTOs;

public record ChangeUserRoleRequestDto
{
    public string Role { get; init; } = string.Empty;
}
