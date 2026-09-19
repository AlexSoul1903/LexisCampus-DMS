using LexisCampusDMS.Application.Interfaces;
using QRCoder;

namespace LexisCampusDMS.Infraestructure.Shared.Services;

public class QrCodeService : IQrCodeService
{
    private const string BaseVerificationUrl = "https://lexiscampus.edu/verify/";
    private static readonly QRCodeGenerator Generator = new();

    public byte[] GeneratePng(string content, int pixelsPerModule = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        using var qrCodeData = Generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    public byte[] GenerateBmp(string content, int pixelsPerModule = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        using var qrCodeData = Generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new BitmapByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    public string GenerateSvg(string content, int pixelsPerModule = 10)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        using var qrCodeData = Generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }

    public byte[] GenerateVerificationQrPng(string fileHashSha256, int pixelsPerModule = 10)
    {
        var url = BuildVerificationUrl(fileHashSha256);
        return GeneratePng(url, pixelsPerModule);
    }

    public byte[] GenerateVerificationQrBmp(string fileHashSha256, int pixelsPerModule = 10)
    {
        var url = BuildVerificationUrl(fileHashSha256);
        return GenerateBmp(url, pixelsPerModule);
    }

    public string GenerateVerificationQrSvg(string fileHashSha256, int pixelsPerModule = 10)
    {
        var url = BuildVerificationUrl(fileHashSha256);
        return GenerateSvg(url, pixelsPerModule);
    }

    public string BuildVerificationUrl(string fileHashSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHashSha256, nameof(fileHashSha256));
        return $"{BaseVerificationUrl}{fileHashSha256.Trim()}";
    }
}
