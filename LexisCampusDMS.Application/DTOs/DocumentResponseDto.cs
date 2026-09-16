using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Application.DTOs;

public record DocumentResponseDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string StudentRegistration { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string DocumentTypeName => DocumentType.ToString();
    public DocumentStatus Status { get; init; }
    public int CurrentVersion { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string CurrentFileHash { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public IReadOnlyList<DocumentVersionDto>? Versions { get; init; }
}
