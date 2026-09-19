using LexisCampusDMS.Infraestructure.Shared.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace LexisCampusDMS.UnitTests.Shared;

public class PdfStamperServiceTests
{
    private readonly PdfStamperService _stamperService = new();
    private readonly QrCodeService _qrCodeService = new();

    [Fact]
    public void StampQrCode_ValidPdfAndQrCode_ProducesStampedPdfWithPdfHeader()
    {
        // Arrange: generate a simple in-memory PDF
        using var pdfDoc = new PdfDocument();
        var page = pdfDoc.AddPage();
        using var pdfMs = new MemoryStream();
        pdfDoc.Save(pdfMs);
        pdfMs.Position = 0;

        // Generate a QR code BMP
        var qrBmp = _qrCodeService.GenerateVerificationQrBmp("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", pixelsPerModule: 4);

        // Act
        var stampedBytes = _stamperService.StampQrCode(
            pdfMs, 
            qrBmp, 
            "https://lexiscampus.edu/verify/e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", 
            "Documento Oficial Certificado");

        // Assert
        Assert.NotNull(stampedBytes);
        Assert.True(stampedBytes.Length > pdfMs.Length);

        // Verify PDF signature (%PDF-)
        var header = System.Text.Encoding.ASCII.GetString(stampedBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);

        // Verify PDF is valid and readable by PdfReader
        using var stampedMs = new MemoryStream(stampedBytes);
        using var reopenedDoc = PdfReader.Open(stampedMs, PdfDocumentOpenMode.Import);
        Assert.Equal(1, reopenedDoc.PageCount);
    }

    [Fact]
    public void StampQrCode_NullStream_ThrowsArgumentNullException()
    {
        var qrPng = new byte[] { 0x89, 0x50 };
        Assert.Throws<ArgumentNullException>(() => _stamperService.StampQrCode(null!, qrPng));
    }

    [Fact]
    public void StampQrCode_NullQrBytes_ThrowsArgumentNullException()
    {
        using var ms = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() => _stamperService.StampQrCode(ms, null!));
    }
}
