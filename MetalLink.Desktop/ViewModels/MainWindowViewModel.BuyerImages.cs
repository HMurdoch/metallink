using System.Threading.Tasks;
using MetalLink.Shared.Buyers;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private async Task LoadSelectedBuyerImagesAsync(BuyerDto? buyer)
    {
        // Always clear first so switching selection doesn't keep stale images
        SelectedIdCardImage = null;
        SelectedDriverLicenseImage = null;
        SelectedPhotoImage = null;
        SelectedSignatureImage = null;
        SelectedFingerprintImage = null;

        IdCardImage = null;
        DriverLicenseImage = null;
        PhotoImage = null;
        SignatureImage = null;
        FingerprintImage = null;

        if (buyer == null)
            return;

        try
        {
            var buyerId = buyer.BuyerId;
            SelectedIdCardImage = LoadBitmapFromBytes(await _buyerService.DownloadBuyerImageAsync(buyerId, "idcard"));
            SelectedDriverLicenseImage = LoadBitmapFromBytes(await _buyerService.DownloadBuyerImageAsync(buyerId, "driverlicense"));
            SelectedPhotoImage = LoadBitmapFromBytes(await _buyerService.DownloadBuyerImageAsync(buyerId, "photo"));
            SelectedSignatureImage = LoadBitmapFromBytes(await _buyerService.DownloadBuyerImageAsync(buyerId, "signature"));
            SelectedFingerprintImage = LoadBitmapFromBytes(await _buyerService.DownloadBuyerImageAsync(buyerId, "fingerprint"));

            // Mirror into Create/Edit panel
            IdCardImage = SelectedIdCardImage;
            DriverLicenseImage = SelectedDriverLicenseImage;
            PhotoImage = SelectedPhotoImage;
            SignatureImage = SelectedSignatureImage;
            FingerprintImage = SelectedFingerprintImage;
        }
        catch
        {
            // optional
        }
    }
}
