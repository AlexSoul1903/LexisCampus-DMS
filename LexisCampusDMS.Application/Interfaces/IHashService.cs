namespace LexisCampusDMS.Application.Interfaces;

public interface IHashService
{
    string ComputeSha256(Stream fileStream);
    Task<string> ComputeSha256Async(Stream fileStream, CancellationToken cancellationToken = default);
    bool VerifySha256(Stream fileStream, string expectedHash);
}
