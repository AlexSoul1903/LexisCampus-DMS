using LexisCampusDMS.Application.Interfaces;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace LexisCampusDMS.Infraestructure.Shared.Services;

public class PdfStamperService : IPdfStamperService
{
    public byte[] StampQrCode(
        Stream inputPdfStream, 
        byte[] qrCodePngBytes, 
        string? verificationUrl = null, 
        string? footerLegend = null)
    {
        ArgumentNullException.ThrowIfNull(inputPdfStream, nameof(inputPdfStream));
        ArgumentNullException.ThrowIfNull(qrCodePngBytes, nameof(qrCodePngBytes));

        if (inputPdfStream.CanSeek && inputPdfStream.Position != 0)
        {
            inputPdfStream.Position = 0;
        }

        using var document = PdfReader.Open(inputPdfStream, PdfDocumentOpenMode.Modify);
        if (document.PageCount == 0)
        {
            document.AddPage();
        }

        var page = document.Pages[document.PageCount - 1];
        using var gfx = XGraphics.FromPdfPage(page);

        // Position in bottom right corner with margin
        double qrSize = 70; // 70pt (~25mm)
        double margin = 20; // 20pt margin
        double x = page.Width.Point - qrSize - margin;
        double y = page.Height.Point - qrSize - margin;

        using var imageStream = new MemoryStream(qrCodePngBytes);
        using var image = XImage.FromStream(imageStream);
        gfx.DrawImage(image, x, y, qrSize, qrSize);

        using var outputMs = new MemoryStream();
        document.Save(outputMs);
        return outputMs.ToArray();
    }
}
