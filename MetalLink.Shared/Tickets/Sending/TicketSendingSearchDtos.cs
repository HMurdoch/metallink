using System;
using System.Text.Json.Serialization;

namespace MetalLink.Shared.Tickets.Sending;

public sealed class TicketSendingSearchResultDto
{
    public long TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public int TicketTypeId { get; set; }

    public long BuyerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }

    [JsonConverter(typeof(AccountNumberConverter))]
    public string? AccountNumber { get; set; }

    public decimal NetWeightKg { get; set; }

    public char? TicketStatus { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
}
