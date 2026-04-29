using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Tickets.Sending;

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
    public async Task<(IReadOnlyList<TicketSendingSearchResultDto> Items, int TotalCount)> SearchTicketsSendingAsync(
        TicketSendingSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<TicketSendingSearchRequestDto, PagedResult<TicketSendingSearchResultDto>>(
            "api/tickets-sending/search",
            request,
            cancellationToken
        );

        return (result?.Items ?? Array.Empty<TicketSendingSearchResultDto>(), result?.TotalCount ?? 0);
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
        // Requirement: compute next ticket number by looking at the LAST STORED ticket number in the table.
        // Example: last stored SWB-00000033 -> next SWB-00000034.

        prefix = prefix.ToUpperInvariant();

        try
        {
            var resp = await _apiClient.GetAsync<System.Collections.Generic.Dictionary<string, object>>(
                $"api/tickets-sending/last-stored-ticket-number/{prefix}");

            string? last = null;
            if (resp != null && resp.TryGetValue("ticketNumber", out var lastObj))
                last = lastObj?.ToString();

            if (string.IsNullOrWhiteSpace(last))
                return $"{prefix}-00000001";

            var parts = last.Split('-');
            var numericPart = parts.Length >= 2 ? parts[^1] : "";
            var width = numericPart.Length > 0 ? numericPart.Length : 8;

            if (!long.TryParse(numericPart, out var lastNumber))
                return $"{prefix}-00000001";

            var next = lastNumber + 1;
            return $"{prefix}-{next.ToString().PadLeft(width, '0')}";
        }
        catch
        {
            return $"{prefix}-00000001";
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

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
    }
}
