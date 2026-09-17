namespace LexisCampusDMS.Core.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public virtual string ErrorCode { get; protected set; } = "DOMAIN_ERROR";

    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
