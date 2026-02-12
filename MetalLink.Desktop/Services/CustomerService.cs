using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Customers;

namespace MetalLink.Desktop.Services;

public sealed class CustomerService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public CustomerService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    public Task<CustomerDto[]?> SearchCustomersAsync(
        CustomerSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<CustomerDto[]?>(
            "api/customers/search" + _apiClient.ToQueryString(request),
            cancellationToken);
    }

    public Task<CustomerDto?> GetCustomerByIdAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<CustomerDto>(
            $"api/customers/{customerId}",
            cancellationToken);
    }

    public Task<CustomerDto?> CreateCustomerAsync(
        CustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.PostAsync<CustomerDto, CustomerDto>(
            "api/customers",
            dto,
            cancellationToken);
    }

    public async Task UpdateCustomerAsync(
        CustomerDto dto,
        CancellationToken cancellationToken = default)
    {
        // If your API route is PUT api/customers (as you’re using)
        var response = await _apiClient.PutAsJsonAsync("api/customers", dto, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SoftDeleteCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        await _apiClient.DeleteAsync($"api/customers/{customerId}", cancellationToken);
    }


    public Task<long> GetNextAccountNumberAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<long>(
            "api/customers/next-account-number",
            cancellationToken)!;
    }

    /// <summary>
    /// Uploads a customer image to the server and returns the storage path
    /// </summary>
    public async Task<string?> UploadCustomerImageAsync(
        long customerId,
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
            $"api/customers/{customerId}/images/{imageType}",
            request,
            cancellationToken);

        return response?.ImagePath;
    }

    /// <summary>
    /// Downloads a customer image from the server
    /// </summary>
    public async Task<byte[]?> DownloadCustomerImageAsync(
        long customerId,
        string imageType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetAsync<byte[]>(
                $"api/customers/{customerId}/images/{imageType}",
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
