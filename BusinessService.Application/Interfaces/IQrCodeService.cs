namespace BusinessService.Application.Interfaces;

public interface IQrCodeService
{
    string GenerateQrCodeBase64(string content);
}