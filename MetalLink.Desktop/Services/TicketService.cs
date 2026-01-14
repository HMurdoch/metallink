using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.Services;

/// <summary>
/// LEGACY: This service uses the old unified Ticket system.
/// Used primarily for search/history functionality that queries both receiving and sending tickets.
/// For new ticket creation, use TicketReceivingService or TicketSendingService instead.
/// </summary>
[Obsolete("Use TicketReceivingService or TicketSendingService for new development")]
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
        string? vehicleRegistration,
        string? ofmWeighbridgeTicket,
        string? foreignTicket,
        string? ckNumber,
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
            notes,
            vehicleRegistration,
            ofmWeighbridgeTicket,
            foreignTicket,
            ckNumber
        };

        return await _apiClient.PostAsync<object, TicketDto>(
            "api/tickets",
            body,
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<TicketLineDto>?> AddTicketLinesAsync(
        long ticketId,
        IEnumerable<(long ProductId, decimal WeightKg)> lines,
        CancellationToken cancellationToken = default)
    {
        var payload = lines
            .Select(l => new { productId = l.ProductId, weightKg = l.WeightKg })
            .ToArray();

        var result = await _apiClient.PostAsync<object, TicketLineDto[]>(
            $"api/tickets/{ticketId}/lines",
            payload,
            cancellationToken
        );

        return result ?? Array.Empty<TicketLineDto>();
    }

    public async Task<IReadOnlyList<TicketLineDto>?> GetTicketLinesAsync(
        long ticketId,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.GetAsync<TicketLineDto[]>(
            $"api/tickets/{ticketId}/lines",
            cancellationToken);

        return result ?? Array.Empty<TicketLineDto>();
    }

    public Task DeleteTicketLineAsync(
        long ticketId,
        long ticketLineId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAsync(
            $"api/tickets/{ticketId}/lines/{ticketLineId}",
            cancellationToken);
    }

    public async Task<bool> UpdateTicketLineAsync(
        long ticketId,
        long ticketLineId,
        long productId,
        decimal weightKg,
        decimal unitPricePerKg,
        CancellationToken cancellationToken = default)
    {
        var body = new
        {
            productId,
            weightKg,
            unitPricePerKg
        };

        try
        {
            var response = await _apiClient.PutAsJsonAsync(
                $"api/tickets/{ticketId}/lines/{ticketLineId}",
                body,
                cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<TicketSearchResultDto>> SearchTicketsAsync(
        TicketSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<TicketSearchRequestDto, TicketSearchResultDto[]>(
            "api/tickets/search",
            request,
            cancellationToken);

        return result ?? Array.Empty<TicketSearchResultDto>();
    }

    public async Task<TicketDto?> GetTicketByIdAsync(
        long ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<TicketDto?>(
            $"api/tickets/{ticketId}",
            cancellationToken);
    }

    public Task DeleteTicketAsync(
        long ticketId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAsync(
            $"api/tickets/{ticketId}",
            cancellationToken);
    }
}
