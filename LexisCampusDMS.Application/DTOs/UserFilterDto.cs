namespace LexisCampusDMS.Application.DTOs;

public record UserFilterDto
{
    public string? SearchTerm { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
