using LexisCampusDMS.Infraestructure.Shared.Services;
using Xunit;

namespace LexisCampusDMS.UnitTests.Shared;

public class QrCodeServiceTests
{
    private readonly QrCodeService _qrCodeService = new();

    [Fact]
    public void BuildVerificationUrl_AppendsHashProperly()
    {
        // Arrange
        var testHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var url = _qrCodeService.BuildVerificationUrl(testHash);

        // Assert
        Assert.Equal($"https://lexiscampus.edu/verify/{testHash}", url);
    }

    [Fact]
    public void GenerateVerificationQrPng_ValidHash_ReturnsValidPngWithHeader()
    {
        // Arrange
        var testHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var pngBytes = _qrCodeService.GenerateVerificationQrPng(testHash, pixelsPerModule: 5);

        // Assert
        Assert.NotNull(pngBytes);
        Assert.True(pngBytes.Length > 100);

        // Check PNG signature: 89 50 4E 47 0D 0A 1A 0A
        byte[] expectedHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Assert.Equal(expectedHeader, pngBytes.Take(8));
    }

    [Fact]
    public void GenerateVerificationQrSvg_ValidHash_ReturnsValidSvgString()
    {
        // Arrange
        var testHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";

        // Act
        var svgString = _qrCodeService.GenerateVerificationQrSvg(testHash, pixelsPerModule: 5);

        // Assert
        Assert.NotNull(svgString);
        Assert.Contains("<svg", svgString);
        Assert.Contains("</svg>", svgString);
        Assert.Contains("viewBox", svgString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BuildVerificationUrl_EmptyOrNullHash_ThrowsArgumentException(string? invalidHash)
    {
        Assert.ThrowsAny<ArgumentException>(() => _qrCodeService.BuildVerificationUrl(invalidHash!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GeneratePng_EmptyOrNullContent_ThrowsArgumentException(string? invalidContent)
    {
        Assert.ThrowsAny<ArgumentException>(() => _qrCodeService.GeneratePng(invalidContent!));
    }
}
