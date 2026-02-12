using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Buyers;

namespace MetalLink.Desktop.Services;

public sealed class BuyerService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public BuyerService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    public Task<BuyerDto[]?> SearchBuyersAsync(
        BuyerSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<BuyerDto[]?>(
            "api/buyers/search" + _apiClient.ToQueryString(request),
            cancellationToken);
    }

    public Task<BuyerDto?> GetBuyerByIdAsync(
        long buyerId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<BuyerDto>(
            $"api/buyers/{buyerId}",
            cancellationToken);
    }

    public Task<BuyerDto?> CreateBuyerAsync(
        BuyerDto dto,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.PostAsync<BuyerDto, BuyerDto>(
            "api/buyers",
            dto,
            cancellationToken);
    }

    public async Task UpdateBuyerAsync(
        BuyerDto dto,
        CancellationToken cancellationToken = default)
    {
        // If your API route is PUT api/buyers (as you’re using)
        var response = await _apiClient.PutAsJsonAsync("api/buyers", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SoftDeleteBuyerAsync(
        long buyerId,
        CancellationToken cancellationToken = default)
    {
        await _apiClient.DeleteAsync($"api/buyers/{buyerId}", cancellationToken);
    }


    public Task<long> GetNextAccountNumberAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<long>(
            "api/buyers/next-account-number",
            cancellationToken)!;
    }

    /// <summary>
    /// Uploads a buyer image to the server and returns the storage path
    /// </summary>
    public async Task<string?> UploadBuyerImageAsync(
        long buyerId,
        string imageType,
        byte[] imageData,
        string contentType = "image/png",
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            ImageData = imageData,
            ContentType = contentType
        };

        var response = await _apiClient.PostAsync<object, UploadImageResponse>(
            $"api/buyers/{buyerId}/images/{imageType}",
            request,
            cancellationToken);

        return response?.ImagePath;
    }

    /// <summary>
    /// Downloads a buyer image from the server
    /// </summary>
    public async Task<byte[]?> DownloadBuyerImageAsync(
        long buyerId,
        string imageType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetAsync<byte[]>(
                $"api/buyers/{buyerId}/images/{imageType}",
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private sealed class UploadImageResponse
    {
        public string? ImagePath { get; set; }
    }
}
