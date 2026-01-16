using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Tickets;

namespace MetalLink.Desktop.Services;

/// <summary>
/// Service for managing sending tickets (selling to buyers/customers)
/// </summary>
public sealed class TicketSendingService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public TicketSendingService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    /// <summary>
    /// Create a new sending ticket
    /// </summary>
    public async Task<TicketSendingDto?> CreateTicketSendingAsync(
        CreateTicketSendingDto createDto,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<CreateTicketSendingDto, TicketSendingDto>(
            "api/tickets-sending",
            createDto,
            cancellationToken
        );
    }

    /// <summary>
    /// Get a sending ticket by ID
    /// </summary>
    public async Task<TicketSendingDto?> GetTicketSendingByIdAsync(
        long ticketSendingId,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<TicketSendingDto>(
            $"api/tickets-sending/{ticketSendingId}",
            cancellationToken
        );
    }

    /// <summary>
    /// Search for sending tickets
    /// </summary>
    public async Task<IReadOnlyList<TicketSearchResultDto>> SearchTicketsSendingAsync(
        TicketSendingSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<TicketSendingSearchRequestDto, TicketSearchResultDto[]>(
            "api/tickets-sending/search",
            request,
            cancellationToken
        );

        return result ?? Array.Empty<TicketSearchResultDto>();
    }

    /// <summary>
    /// Update a sending ticket
    /// </summary>
    public async Task<TicketSendingDto?> UpdateTicketSendingAsync(
        long ticketSendingId,
        CreateTicketSendingDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"api/tickets-sending/{ticketSendingId}",
            updateDto,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TicketSendingDto>(cancellationToken: cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Delete a sending ticket
    /// </summary>
    public Task DeleteTicketSendingAsync(
        long ticketSendingId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAsync(
            $"api/tickets-sending/{ticketSendingId}",
            cancellationToken
        );
    }

    /// <summary>
    /// Add lines to a platform sending ticket
    /// </summary>
    public async Task<IReadOnlyList<TicketSendingLineDto>?> AddTicketSendingLinesAsync(
        long ticketSendingId,
        IEnumerable<(long ProductId, decimal WeightKg, decimal UnitPricePerKg)> lines,
        CancellationToken cancellationToken = default)
    {
        var payload = lines
            .Select(l => new { productId = l.ProductId, weightKg = l.WeightKg, unitPricePerKg = l.UnitPricePerKg })
            .ToArray();

        var result = await _apiClient.PostAsync<object, TicketSendingLineDto[]>(
            $"api/tickets-sending/{ticketSendingId}/lines",
            payload,
            cancellationToken
        );

        return result ?? Array.Empty<TicketSendingLineDto>();
    }

    /// <summary>
    /// Get lines for a sending ticket
    /// </summary>
    public async Task<IReadOnlyList<TicketSendingLineDto>?> GetTicketSendingLinesAsync(
        long ticketSendingId,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.GetAsync<TicketSendingLineDto[]>(
            $"api/tickets-sending/{ticketSendingId}/lines",
            cancellationToken
        );

        return result ?? Array.Empty<TicketSendingLineDto>();
    }

    /// <summary>
    /// Update a line in a sending ticket
    /// </summary>
    public async Task<bool> UpdateTicketSendingLineAsync(
        long ticketSendingId,
        long ticketSendingLineId,
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
                $"api/tickets-sending/{ticketSendingId}/lines/{ticketSendingLineId}",
                body,
                cancellationToken
            );

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Delete a line from a sending ticket
    /// </summary>
    public Task DeleteTicketSendingLineAsync(
        long ticketSendingId,
        long ticketSendingLineId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAsync(
            $"api/tickets-sending/{ticketSendingId}/lines/{ticketSendingLineId}",
            cancellationToken
        );
    }

    /// <summary>
    /// Update delivery status of a sending ticket
    /// </summary>
    public async Task<bool> UpdateDeliveryStatusAsync(
        long ticketSendingId,
        string deliveryStatus,
        CancellationToken cancellationToken = default)
    {
        var body = new { deliveryStatus };

        try
        {
            var response = await _apiClient.PutAsJsonAsync(
                $"api/tickets-sending/{ticketSendingId}/status",
                body,
                cancellationToken
            );

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
