namespace LexisCampusDMS.Application.Interfaces;

public interface IQrCodeService
{
    byte[] GeneratePng(string content, int pixelsPerModule = 10);
    byte[] GenerateBmp(string content, int pixelsPerModule = 10);
    string GenerateSvg(string content, int pixelsPerModule = 10);
    byte[] GenerateVerificationQrPng(string fileHashSha256, int pixelsPerModule = 10);
    byte[] GenerateVerificationQrBmp(string fileHashSha256, int pixelsPerModule = 10);
    string GenerateVerificationQrSvg(string fileHashSha256, int pixelsPerModule = 10);
    string BuildVerificationUrl(string fileHashSha256);
}
