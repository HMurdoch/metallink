namespace MetalLink.Shared.Tickets;

public sealed class TicketDto
{
    public long TicketId { get; set; }
    public long SiteId { get; set; }
    public long CustomerId { get; set; }  // For receiving tickets
    public long? BuyerId { get; set; }     // For delivery/sending tickets
    public long OperatorId { get; set; }

    public string TicketNumber { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;

    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }

    public decimal UnitPricePerKg { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";

    public long? CurrencyId { get; set; }
    public long? ProductId { get; set; }

    // Header / vehicle details
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    public string? OfmWeighbridgeTicket { get; set; }
    public string? ForeignTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    
    // Ticket status and RFID
    public string Status { get; set; } = "receiving";
    public string? RfidCardNumber { get; set; }

    // VAT
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }

    public string? ProductDescription { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    // Line items
    public ICollection<TicketLineDto> Lines { get; set; } = new List<TicketLineDto>();
}
