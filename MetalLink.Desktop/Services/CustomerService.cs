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

    public async Task<CustomerDto?> GetCustomerByIdAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<CustomerDto>(
            $"api/customers/{customerId}",
            cancellationToken);
    }

    public async Task<CustomerDto?> CreateCustomerAsync(
        string fullName,
        bool isCompany,
        string? companyName,
        string? idNumber,
        string? accountNumber,
        string? priceCode,
        string? addressLine1,
        string? addressLine2,
        string? suburb,
        string? city,
        string? postalCode,
        string? phoneNumber,
        string? mobileNumber,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var siteId = _authState.SiteId > 0 ? _authState.SiteId : 1; // fallback

        // Shape matches API CreateCustomerRequest
        var body = new
        {
            siteId,
            fullName,
            isCompany,
            companyName,
            idNumber,
            accountNumber,
            priceCode,
            addressLine1,
            addressLine2,
            suburb,
            city,
            postalCode,
            phoneNumber,
            mobileNumber,
            email
        };

        return await _apiClient.PostAsync<object, CustomerDto>(
            "api/customers",
            body,
            cancellationToken);
    }
}
