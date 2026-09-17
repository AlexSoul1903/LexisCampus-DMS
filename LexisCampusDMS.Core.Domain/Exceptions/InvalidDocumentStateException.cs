using LexisCampusDMS.Core.Domain.Enums;

namespace LexisCampusDMS.Core.Domain.Exceptions;

public class InvalidDocumentStateException : DomainException
{
    public override string ErrorCode { get; protected set; } = "INVALID_DOCUMENT_STATE";
    public DocumentStatus CurrentStatus { get; }
    public DocumentStatus TargetStatus { get; }

    public InvalidDocumentStateException(DocumentStatus currentStatus, DocumentStatus targetStatus)
        : base($"No se puede realizar la transición de estado del documento de '{currentStatus}' a '{targetStatus}'.")
    {
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }

    public InvalidDocumentStateException(string message) : base(message)
    {
    }
}
