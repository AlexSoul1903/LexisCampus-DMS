namespace LexisCampusDMS.Core.Domain.Exceptions;

public class DomainValidationException : DomainException
{
    public override string ErrorCode { get; protected set; } = "DOMAIN_VALIDATION_ERROR";
    public IReadOnlyList<string> Errors { get; }

    public DomainValidationException(string message) : base(message)
    {
        Errors = [message];
    }

    public DomainValidationException(string propertyName, string error)
        : base($"Validación de dominio fallida en '{propertyName}': {error}")
    {
        Errors = [$"{propertyName}: {error}"];
    }

    public DomainValidationException(IEnumerable<string> errors)
        : base("Se presentaron uno o más errores de validación en las reglas de dominio.")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}
