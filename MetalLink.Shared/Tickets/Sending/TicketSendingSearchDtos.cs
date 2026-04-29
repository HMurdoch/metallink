using System;
using System.Text.Json.Serialization;

namespace MetalLink.Shared.Tickets.Sending;

public sealed class TicketSendingSearchResultDto
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public int TicketTypeId { get; set; }

    public int BuyerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }

    [JsonConverter(typeof(AccountNumberConverter))]
    public string? AccountNumber { get; set; }

    public decimal NetWeightKg { get; set; }

    public string? ProductGroupName { get; set; }

    public char? TicketStatus { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
}

public class TicketSendingSearchRequestDto
{
    public bool NewBuyerOnly { get; set; }
    public string? SearchTerm { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public int? BuyerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? IdNumber { get; set; }
    public long? AccountNumber { get; set; }
    public int? ProductId { get; set; }
    public int? ProductGroupId { get; set; }
    public string? TicketType { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? SortBy { get; set; } = "ticket_number";
    public string? SortDirection { get; set; } = "desc";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
