namespace LexisCampusDMS.Application.Interfaces;

public interface IPdfStamperService
{
    byte[] StampQrCode(
        Stream inputPdfStream, 
        byte[] qrCodePngBytes, 
        string? verificationUrl = null, 
        string? footerLegend = null);
}
