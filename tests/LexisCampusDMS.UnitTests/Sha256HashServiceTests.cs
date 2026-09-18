using System.Text;
using LexisCampusDMS.Infraestructure.Shared.Services;
using Xunit;

namespace LexisCampusDMS.UnitTests.Shared;

public class Sha256HashServiceTests
{
    private readonly Sha256HashService _hashService = new();

    [Fact]
    public void ComputeSha256_ShouldReturnLowercase64CharacterHexadecimalString()
    {
        // Arrange
        var content = "LexisCampus DMS Document Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = _hashService.ComputeSha256(stream);

        // Assert: 64 characters, hexadecimal, lowercase
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[a-f0-9]{64}$", hash);
    }

    [Fact]
    public async Task ComputeSha256Async_ShouldReturnSameHashNonBlocking()
    {
        // Arrange
        var content = "LexisCampus DMS Document Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var syncHash = _hashService.ComputeSha256(stream);
        var asyncHash = await _hashService.ComputeSha256Async(stream);

        // Assert
        Assert.Equal(syncHash, asyncHash);
    }

    [Fact]
    public async Task ComputeSha256_ShouldPreserveStreamPositionAtZero()
    {
        // Arrange: Technical note: "Garantizar que el stream de entrada mantenga su Position = 0 si requiere ser reutilizado para el upload a MinIO."
        var content = "LexisCampus DMS Stream Position Reusability Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Start at position 0
        Assert.Equal(0, stream.Position);

        // Act sync
        _hashService.ComputeSha256(stream);
        Assert.Equal(0, stream.Position);

        // Advance position manually to simulate prior read
        stream.Position = 10;

        // Act async
        await _hashService.ComputeSha256Async(stream);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task VerifySha256_ShouldReturnTrueForMatchingContent()
    {
        // Arrange
        var content = "Authentic Student Document PDF Content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var validHash = await _hashService.ComputeSha256Async(stream);

        // Act & Assert
        Assert.True(_hashService.VerifySha256(stream, validHash));
        Assert.True(await _hashService.VerifySha256Async(stream, validHash));
        // Verify case-insensitivity on comparison
        Assert.True(_hashService.VerifySha256(stream, validHash.ToUpperInvariant()));
    }

    [Fact]
    public async Task VerifySha256_ShouldReturnFalseForTamperedContentOrMismatch()
    {
        // Arrange
        var originalContent = "Original Authentic Grades Transcript";
        using var originalStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        var originalHash = await _hashService.ComputeSha256Async(originalStream);

        var tamperedContent = "Tampered Modified Grades Transcript";
        using var tamperedStream = new MemoryStream(Encoding.UTF8.GetBytes(tamperedContent));

        // Act & Assert
        Assert.False(_hashService.VerifySha256(tamperedStream, originalHash));
        Assert.False(await _hashService.VerifySha256Async(tamperedStream, originalHash));
        Assert.False(_hashService.VerifySha256(tamperedStream, "invalid-hash"));
        Assert.False(_hashService.VerifySha256(tamperedStream, string.Empty));
    }
}
