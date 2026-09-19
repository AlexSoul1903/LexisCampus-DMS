namespace LexisCampusDMS.Application.DTOs;

public record DocumentVerificationResponseDto
{
    public Guid DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string StudentRegistration { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsRevoked { get; init; }
    public bool IsValid { get; init; }
    public string? WarningSeal { get; init; }
    public string? ResolutionNumber { get; init; }
    public string? RevocationReason { get; init; }
    public string? RevocationObservations { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
    public string? RevokedBy { get; init; }
    public int CurrentVersion { get; init; }
    public string CurrentFileHash { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime VerificationTimestampUtc { get; init; } = DateTime.UtcNow;
}
