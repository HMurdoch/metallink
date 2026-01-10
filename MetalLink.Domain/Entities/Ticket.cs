using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalLink.Domain.Entities;

public class Ticket
{
    public long TicketId { get; private set; }
    public long SiteId { get; private set; }
    public Site Site       { get; set; } = null!;
    public long CustomerId { get; private set; }
    public Customer Customer { get; set; } = null!;
    public long OperatorId { get; private set; }
    public Operator Operator { get; set; } = null!;

    // Optional header-level product and currency (normalized via FKs)
    public long? ProductId { get; private set; }
    public Product? Product { get; set; }

    public long? CurrencyId { get; private set; }
    public Currency? Currency { get; set; }

    public string TicketNumber { get; private set; } = string.Empty; // e.g. "WB-2025-00001"
    public string TicketType { get; private set; } = "weighbridge";  // "weighbridge" or "platform"

    // Header / vehicle details
    public string? VehicleRegistration { get; private set; }
    public string? TrailerRegistration { get; private set; }
    public string? DriverName { get; private set; }
    public string? OfmWeighbridgeTicket { get; private set; }
    public string? ForeignTicket { get; private set; }
    public string? CkNumber { get; private set; }

    // Weights
    public decimal? FirstWeightKg { get; private set; }   // weighbridge: gross, platform: net
    public decimal? SecondWeightKg { get; private set; }  // weighbridge: tare, platform: null
    public decimal NetWeightKg { get; private set; }

    // Pricing (always ex-VAT per kg)
    public decimal UnitPricePerKg { get; private set; }   // ex-VAT currency per kg
    public decimal TotalAmount { get; private set; }      // ex-VAT = NetWeightKg * UnitPricePerKg
    public string CurrencyCode { get; private set; } = "ZAR";

    // VAT (for receiving)
    public decimal VatRate { get; private set; } = 0.15m; // 15%
    public decimal VatAmount { get; private set; }
    public decimal TotalInclVat { get; private set; }

    public string? ProductDescription { get; private set; }
    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    // Navigation
    public ICollection<TicketLine> Lines { get; private set; } = new List<TicketLine>();

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
        string? notes,
        string? vehicleRegistration = null,
        string? trailerRegistration = null,
        string? driverName = null,
        string? ofmWeighbridgeTicket = null,
        string? foreignTicket = null,
        string? ckNumber = null,
        long? productId = null,
        long? currencyId = null)
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
        ProductId = productId;
        CurrencyId = currencyId;

        VehicleRegistration = vehicleRegistration;
        TrailerRegistration = trailerRegistration;
        DriverName = driverName;
        OfmWeighbridgeTicket = ofmWeighbridgeTicket;
        ForeignTicket = foreignTicket;
        CkNumber = ckNumber;

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

        // Ex-VAT total
        TotalAmount = decimal.Round(NetWeightKg * UnitPricePerKg, 2, MidpointRounding.AwayFromZero);

        // VAT and total incl VAT (for receiving tickets)
        VatAmount = decimal.Round(TotalAmount * VatRate, 2, MidpointRounding.AwayFromZero);
        TotalInclVat = TotalAmount + VatAmount;
    }

    public void UpdateTotalsFromLines(decimal vatRate, decimal totalExcl, decimal totalVat, decimal totalIncl)
    {
        VatRate = vatRate;
        TotalAmount = totalExcl;
        VatAmount = totalVat;
        TotalInclVat = totalIncl;
        Touch();
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedTime = now;

        if (Lines is null)
        {
            return;
        }

        foreach (var line in Lines.Where(l => l.IsActive))
        {
            line.IsActive = false;
            line.UpdatedTime = now;
        }
    }

    private void Touch()
    {
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
