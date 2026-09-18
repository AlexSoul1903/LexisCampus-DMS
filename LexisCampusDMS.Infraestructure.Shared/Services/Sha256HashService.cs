using System.Security.Cryptography;
using LexisCampusDMS.Application.Interfaces;

namespace LexisCampusDMS.Infraestructure.Shared.Services;

public class Sha256HashService : IHashService
{
    public string ComputeSha256(Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileStream);

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<string> ComputeSha256Async(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool VerifySha256(Stream fileStream, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
            return false;

        var actualHash = ComputeSha256(fileStream);
        return string.Equals(actualHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> VerifySha256Async(Stream fileStream, string expectedHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
            return false;

        var actualHash = await ComputeSha256Async(fileStream, cancellationToken);
        return string.Equals(actualHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
