namespace LexisCampusDMS.Core.Domain.Exceptions;

public class FileHashMismatchException : Exception
{
    public string ErrorCode { get; } = "FILE_HASH_MISMATCH";
    public string ExpectedHash { get; }
    public string ActualHash { get; }

    public FileHashMismatchException(string expectedHash, string actualHash)
        : base($"Error de verificación de integridad del archivo. Hash SHA-256 esperado: '{expectedHash}', hash computado: '{actualHash}'.")
    {
        ExpectedHash = expectedHash;
        ActualHash = actualHash;
    }

    public FileHashMismatchException(string message) : base(message)
    {
        ExpectedHash = string.Empty;
        ActualHash = string.Empty;
    }
}
