using BusinessService.Application.Interfaces;
using QRCoder;

namespace BusinessService.Application.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateQrCodeBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(20);

        return Convert.ToBase64String(bytes);
    }
}