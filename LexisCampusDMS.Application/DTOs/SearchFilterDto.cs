using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Application.DTOs;

public record SearchFilterDto
{
    public string? StudentRegistration { get; init; }
    public DocumentType? DocumentType { get; init; }
    public DateTime? FromDateUtc { get; init; }
    public DateTime? ToDateUtc { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
