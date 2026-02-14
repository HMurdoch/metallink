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
    public async Task<NewBuyerResultDto[]?> SearchNewBuyersWithoutTicketsAsync(
        TicketSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<TicketSearchRequestDto, NewBuyerResultDto[]>(
            "api/tickets-sending/search-new-buyers",
            request,
            cancellationToken);
    }

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

    public async Task<bool> UpdateTicketStateAsync(
        long ticketSendingId,
        char newState,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updateDto = new { TicketState = newState };
            var result = await _apiClient.PutAsJsonAsync(
                $"api/tickets-sending/{ticketSendingId}/state",
                updateDto,
                cancellationToken
            );

            return result.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
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
    /// Add a line to a sending ticket
    /// </summary>
    public async Task<TicketSendingDto?> AddTicketSendingLineAsync(
        long ticketSendingId,
        CreateTicketSendingLineDto line,
        CancellationToken cancellationToken = default)
    {
        return await _apiClient.PostAsync<CreateTicketSendingLineDto, TicketSendingDto>(
            $"api/tickets-sending/{ticketSendingId}/lines",
            line,
            cancellationToken
        );
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

    public async Task<bool> UpdateLineTareAsync(
        long ticketSendingId,
        long ticketSendingLineId,
        decimal tare,
        CancellationToken cancellationToken = default)
    {
        var body = new { tare };

        try
        {
            var response = await _apiClient.PutAsJsonAsync(
                $"api/tickets-sending/{ticketSendingId}/lines/{ticketSendingLineId}/tare",
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

    public async Task<string> GenerateTicketNumberAsync(string prefix)
    {
        try
        {
            // Get last ticket number from API
            var result = await _apiClient.GetAsync<System.Collections.Generic.Dictionary<string, object>>($"api/tickets-sending/last-ticket-number/{prefix}");
            
            if (result == null || !result.ContainsKey("ticketNumber"))
            {
                // No previous ticket, start with 00000001
                return $"{prefix}-00000001";
            }

            var lastTicketNumber = result["ticketNumber"]?.ToString();
            if (string.IsNullOrEmpty(lastTicketNumber))
            {
                return $"{prefix}-00000001";
            }
            
            // API now returns the NEXT atomic ticket number.
            return lastTicketNumber;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<string> GetNextTicketNumberAsync(int ticketTypeId)
    {
        try
        {
            var result = await _apiClient.GetAsync<dynamic>($"api/tickets-sending/next-ticket-number/{ticketTypeId}");
            if (result != null)
            {
                var ticketNumber = result.ticketNumber;
                if (ticketNumber != null)
                {
                    return ticketNumber.ToString() ?? string.Empty;
                }
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
