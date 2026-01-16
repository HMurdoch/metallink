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
/// Service for managing receiving tickets (buying from customers/suppliers)
/// </summary>
public sealed class TicketReceivingService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public TicketReceivingService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    /// <summary>
    /// Create a new receiving ticket
    /// </summary>
    public async Task<TicketReceivingDto?> CreateTicketReceivingAsync(
        CreateTicketReceivingDto createDto,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<CreateTicketReceivingDto, TicketReceivingDto>(
            "api/tickets-receiving",
            createDto,
            cancellationToken
        );
    }

    /// <summary>
    /// Get a receiving ticket by ID
    /// </summary>
    public async Task<TicketReceivingDto?> GetTicketReceivingByIdAsync(
        long ticketReceivingId,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAsync<TicketReceivingDto>(
            $"api/tickets-receiving/{ticketReceivingId}",
            cancellationToken
        );
    }

    /// <summary>
    /// Search for receiving tickets
    /// </summary>
    public async Task<IReadOnlyList<TicketSearchResultDto>> SearchTicketsReceivingAsync(
        TicketReceivingSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<TicketReceivingSearchRequestDto, TicketSearchResultDto[]>(
            "api/tickets-receiving/search",
            request,
            cancellationToken
        );

        return result ?? Array.Empty<TicketSearchResultDto>();
    }

    /// <summary>
    /// Update a receiving ticket
    /// </summary>
    public async Task<TicketReceivingDto?> UpdateTicketReceivingAsync(
        long ticketReceivingId,
        CreateTicketReceivingDto updateDto,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"api/tickets-receiving/{ticketReceivingId}",
            updateDto,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TicketReceivingDto>(cancellationToken: cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Delete a receiving ticket
    /// </summary>
    public Task DeleteTicketReceivingAsync(
        long ticketReceivingId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAsync(
            $"api/tickets-receiving/{ticketReceivingId}",
            cancellationToken
        );
    }

    /// <summary>
    /// Add lines to a platform receiving ticket
    /// </summary>
    public async Task<IReadOnlyList<TicketReceivingLineDto>?> AddTicketReceivingLinesAsync(
        long ticketReceivingId,
        IEnumerable<(long ProductId, decimal WeightKg, decimal UnitPricePerKg)> lines,
        CancellationToken cancellationToken = default)
    {
        var payload = lines
            .Select(l => new { productId = l.ProductId, weightKg = l.WeightKg, unitPricePerKg = l.UnitPricePerKg })
            .ToArray();

        var result = await _apiClient.PostAsync<object, TicketReceivingLineDto[]>(
            $"api/tickets-receiving/{ticketReceivingId}/lines",
            payload,
            cancellationToken
        );

        return result ?? Array.Empty<TicketReceivingLineDto>();
    }

    /// <summary>
    /// Get lines for a receiving ticket
    /// </summary>
    public async Task<IReadOnlyList<TicketReceivingLineDto>?> GetTicketReceivingLinesAsync(
        long ticketReceivingId,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.GetAsync<TicketReceivingLineDto[]>(
            $"api/tickets-receiving/{ticketReceivingId}/lines",
            cancellationToken
        );

        return result ?? Array.Empty<TicketReceivingLineDto>();
    }

    /// <summary>
    /// Update a line in a receiving ticket
    /// </summary>
    public async Task<bool> UpdateTicketReceivingLineAsync(
        long ticketReceivingId,
        long ticketReceivingLineId,
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
                $"api/tickets-receiving/{ticketReceivingId}/lines/{ticketReceivingLineId}",
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
    /// Delete a line from a receiving ticket
    /// </summary>
    public Task DeleteTicketReceivingLineAsync(
        long ticketReceivingId,
        long ticketReceivingLineId,
        CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteAsync(
            $"api/tickets-receiving/{ticketReceivingId}/lines/{ticketReceivingLineId}",
            cancellationToken
        );
    }

    /// <summary>
    /// Update delivery status of a receiving ticket
    /// </summary>
    public async Task<bool> UpdateDeliveryStatusAsync(
        long ticketReceivingId,
        string deliveryStatus,
        CancellationToken cancellationToken = default)
    {
        var body = new { deliveryStatus };

        try
        {
            var response = await _apiClient.PutAsJsonAsync(
                $"api/tickets-receiving/{ticketReceivingId}/status",
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
