using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.Services;

public sealed class TicketService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public TicketService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    public async Task<TicketDto?> CreateTicketAsync(
        long customerId,
        string ticketType,          // "weighbridge" or "platform"
        string ticketNumber,
        decimal? firstWeightKg,
        decimal? secondWeightKg,
        decimal unitPricePerKg,
        string currencyCode,
        string? productDescription,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var siteId = _authState.SiteId > 0 ? _authState.SiteId : 1;

        // For now: hard-code OperatorId = 1 (admin).
        // Later we can put OperatorId into the JWT and expose via AuthState.
        var operatorId = 1L;

        var body = new
        {
            siteId,
            customerId,
            operatorId,
            ticketType,
            ticketNumber,
            firstWeightKg,
            secondWeightKg,
            unitPricePerKg,
            currencyCode,
            productDescription,
            notes
        };

        return await _apiClient.PostAsync<object, TicketDto>(
            "api/tickets",
            body,
            cancellationToken
        );
    }
}
