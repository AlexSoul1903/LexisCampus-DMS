using System.Text.Json.Serialization;

namespace LexisCampusDMS.Application.DTOs;

public class PublicVerificationResponseDto
{
    [JsonPropertyName("valido")]
    public bool IsValid { get; set; }

    [JsonPropertyName("titulo")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("matriculaAnonimizada")]
    public string AnonymizedStudentRegistration { get; set; } = string.Empty;

    [JsonPropertyName("fechaEmision")]
    public DateTime IssueDateUtc { get; set; }

    [JsonPropertyName("decanoFirmante")]
    public string SigningDean { get; set; } = string.Empty;

    [JsonPropertyName("selloInstitucional")]
    public string InstitutionalSeal { get; set; } = string.Empty;

    [JsonPropertyName("tipoDocumento")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("hashSha256")]
    public string FileHashSha256 { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("advertenciaRevocacion")]
    public string? RevocationWarning { get; set; }
}
