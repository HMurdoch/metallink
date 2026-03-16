using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MetalLink.Shared.Customers;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private async Task LoadSelectedCustomerImagesAsync(CustomerDto? customer)
    {
        // Always clear first so switching selection doesn't keep stale images
        SelectedIdCardImage = null;
        SelectedDriverLicenseImage = null;
        SelectedPhotoImage = null;
        SelectedSignatureImage = null;
        SelectedFingerprintImage = null;

        // Also clear Create/Edit image previews by default
        SelectedIdCardImage = null;
        SelectedDriverLicenseImage = null;
        SelectedPhotoImage = null;
        SelectedSignatureImage = null;
        SelectedFingerprintImage = null;

        if (customer == null)
            return;

        try
        {
            var customerId = customer.CustomerId;

            // NOTE: These endpoints return bytes (API returns File(imageData, ...))
            SelectedIdCardImage = LoadBitmapFromBytes(await _customerService.DownloadCustomerImageAsync(customerId, "idcard"));
            SelectedDriverLicenseImage = LoadBitmapFromBytes(await _customerService.DownloadCustomerImageAsync(customerId, "driverlicense"));
            SelectedPhotoImage = LoadBitmapFromBytes(await _customerService.DownloadCustomerImageAsync(customerId, "photo"));
            SelectedSignatureImage = LoadBitmapFromBytes(await _customerService.DownloadCustomerImageAsync(customerId, "signature"));
            SelectedFingerprintImage = LoadBitmapFromBytes(await _customerService.DownloadCustomerImageAsync(customerId, "fingerprint"));

            // When editing an existing customer, mirror the loaded images into the Create/Edit panel.
            // This makes images visible under Create/Edit when a customer is selected.
            IdCardImage = SelectedIdCardImage;
            DriverLicenseImage = SelectedDriverLicenseImage;
            PhotoImage = SelectedPhotoImage;
            SignatureImage = SelectedSignatureImage;
            FingerprintImage = SelectedFingerprintImage;
        }
        catch
        {
            // Images are optional; ignore failures.
        }
    }
}
