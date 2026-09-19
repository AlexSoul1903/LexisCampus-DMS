namespace LexisCampusDMS.Application.DTOs;

public record ChangeUserStatusRequestDto
{
    public bool IsActive { get; init; }
    public bool ResetLockout { get; init; }
}
