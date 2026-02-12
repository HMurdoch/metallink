using System;
using System.Text.Json.Serialization;

namespace MetalLink.Shared.Tickets;

public sealed class TicketSearchRequestDto
{
    // Receiving
    public long? CustomerId { get; set; }

    // Sending
    public long? BuyerId { get; set; }
    public string? IdNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string? CompanyLetter { get; set; }
    public long? CompanyId { get; set; }
    public long? SiteId { get; set; }

    public long? AccountNumber { get; set; }

    public string? TicketNumber { get; set; }
    public string? TicketType { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }

    public bool SearchNewCustomersWithoutTickets { get; set; }

    public bool SearchNewBuyersWithoutTickets { get; set; }
}

public sealed class TicketSearchResultDto
{
    public long TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public int TicketTypeId { get; set; }

    public long CustomerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }

    [JsonConverter(typeof(AccountNumberConverter))]
    public string? AccountNumber { get; set; }

    public decimal NetWeightKg { get; set; }
    
    /// <summary>
    /// TODO: Ticket state tracking not yet implemented.
    /// Currently this field is not used.
    /// </summary>
    public char? TicketStatus { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
}
