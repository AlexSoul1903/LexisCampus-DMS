using System.Text.Json.Serialization;

namespace LexisCampusDMS.Application.DTOs;

public record RevokeDocumentDto
{
    public string Reason { get; init; } = string.Empty;
    public string ResolutionNumber { get; init; } = string.Empty;
    public string? Observations { get; init; }

    // Support Spanish parameter names in JSON payloads
    [JsonPropertyName("motivo")]
    public string? Motivo { init => Reason = !string.IsNullOrWhiteSpace(value) ? value : Reason; }

    [JsonPropertyName("numeroResolucion")]
    public string? NumeroResolucion { init => ResolutionNumber = !string.IsNullOrWhiteSpace(value) ? value : ResolutionNumber; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { init => Observations = value; }
}
