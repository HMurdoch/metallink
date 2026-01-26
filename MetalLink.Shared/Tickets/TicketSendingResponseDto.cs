namespace MetalLink.Shared.Tickets;

/// <summary>
/// Response DTO for Sending Tickets with calculated financial totals
/// </summary>
public class TicketSendingResponseDto
{
    public long TicketSendingId { get; set; }
    public long BuyerId { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    
    // Buyer Info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }
    
    // Weight Information
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    
    // Vehicle & Driver Information
    public string? DriverName { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    
    // Ticket Details
    public string? Notes { get; set; }
    public string? OfmWeighbridgeTicket { get; set; }
    public string? DeliveryNumber { get; set; }
    
    // Line Items
    public List<TicketLineResponseDto> Lines { get; set; } = new();
    
    // Metadata
    public long CreatedByOperatorId { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public bool IsActive { get; set; }
}
