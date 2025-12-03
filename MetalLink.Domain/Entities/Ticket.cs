namespace MetalLink.Domain.Entities;

public class Ticket
{
    public long TicketId { get; private set; }
    public long SiteId { get; private set; }
    public long CustomerId { get; private set; }
    public long OperatorId { get; private set; }

    public string TicketNumber { get; private set; } = string.Empty; // e.g. "WB-2025-00001"
    public string TicketType { get; private set; } = "weighbridge";  // "weighbridge" or "platform"

    public decimal? FirstWeightKg { get; private set; }   // weighbridge: gross, platform: net
    public decimal? SecondWeightKg { get; private set; }  // weighbridge: tare, platform: null
    public decimal NetWeightKg { get; private set; }

    public decimal UnitPricePerKg { get; private set; }   // currency per kg
    public decimal TotalAmount { get; private set; }      // NetWeightKg * UnitPricePerKg
    public string CurrencyCode { get; private set; } = "ZAR";

    public string? ProductDescription { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    private Ticket() { }

    public Ticket(
        long siteId,
        long customerId,
        long operatorId,
        string ticketNumber,
        string ticketType,
        decimal? firstWeightKg,
        decimal? secondWeightKg,
        decimal unitPricePerKg,
        string currencyCode,
        string? productDescription,
        string? notes)
    {
        SiteId = siteId;
        CustomerId = customerId;
        OperatorId = operatorId;
        TicketNumber = ticketNumber;
        TicketType = ticketType;
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        UnitPricePerKg = unitPricePerKg;
        CurrencyCode = currencyCode;
        ProductDescription = productDescription;
        Notes = notes;

        CalculateNetAndTotal();
    }

    public void UpdateWeights(decimal? firstWeightKg, decimal? secondWeightKg)
    {
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        CalculateNetAndTotal();
        Touch();
    }

    public void UpdatePrice(decimal unitPricePerKg, string currencyCode)
    {
        UnitPricePerKg = unitPricePerKg;
        CurrencyCode = currencyCode;
        CalculateNetAndTotal();
        Touch();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        Touch();
    }

    private void CalculateNetAndTotal()
    {
        decimal net;

        if (FirstWeightKg is null && SecondWeightKg is null)
        {
            net = 0m;
        }
        else if (FirstWeightKg is not null && SecondWeightKg is not null)
        {
            // Weighbridge: gross - tare
            net = FirstWeightKg.Value - SecondWeightKg.Value;
        }
        else
        {
            // Platform: just first weight
            net = FirstWeightKg ?? 0m;
        }

        if (net < 0)
        {
            net = 0m; // safety guard
        }

        NetWeightKg = net;
        TotalAmount = decimal.Round(NetWeightKg * UnitPricePerKg, 2, MidpointRounding.AwayFromZero);
    }

    private void Touch()
    {
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
