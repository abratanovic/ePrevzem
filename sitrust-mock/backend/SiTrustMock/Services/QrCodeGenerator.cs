using QRCoder;

namespace SiTrustMock;

public class QrCodeGenerator : IQrCodeGenerator
{
    public string Generate(string url)
    {
        var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var bytes = new PngByteQRCode(data).GetGraphic(10);
        return Convert.ToBase64String(bytes);
    }
}
