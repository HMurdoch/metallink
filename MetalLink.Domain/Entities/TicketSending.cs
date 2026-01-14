using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a sending/delivery ticket (selling scrap metal to buyers).
/// Stock OUT operation.
/// </summary>
public class TicketSending
{
    public long TicketSendingId { get; private set; }

    // Company & Site
    public long CompanyId { get; private set; }
    public Company Company { get; set; } = null!;

    public long SiteId { get; private set; }
    public Site Site { get; set; } = null!;

    // Buyer (who we're selling to)
    public long BuyerId { get; private set; }
    public Buyer Buyer { get; set; } = null!;

    // Ticket details
    public string TicketNumber { get; private set; } = string.Empty;
    public string TicketType { get; private set; } = "weighbridge"; // "weighbridge" or "platform"
    
    // Weights (kg)
    public decimal? FirstWeightKg { get; private set; }
    public decimal? SecondWeightKg { get; private set; }
    public decimal NetWeightKg { get; private set; }

    // Financial
    public decimal UnitPricePerKg { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string CurrencyCode { get; private set; } = "ZAR";

    // Product information
    public long? ProductId { get; private set; }
    public Product? Product { get; set; }
    public string? ProductDescription { get; private set; }

    // Vehicle & driver information
    public string? VehicleRegistration { get; private set; }
    public string? TrailerRegistration { get; private set; }
    public string? DriverName { get; private set; }

    // Reference numbers
    public string? OfmWeighbridgeTicket { get; private set; }
    public string? ForeignTicket { get; private set; }
    public string? CkNumber { get; private set; }
    public string? DeliveryNumber { get; private set; }

    // RFID tracking
    public string? RfidTag { get; private set; }
    public DateTimeOffset? RfidFirstScan { get; private set; }
    public DateTimeOffset? RfidSecondScan { get; private set; }

    // Delivery status
    public string DeliveryStatus { get; private set; } = "pending"; // pending, in_progress, completed, cancelled

    // Additional information
    public string? Notes { get; private set; }

    // Photos (S3 keys or URLs)
    public string? PlatePhotoUrl { get; private set; }
    public string? LoadPhotoUrl { get; private set; }

    // Lines (for platform tickets with multiple products)
    public ICollection<TicketSendingLine> Lines { get; set; } = new List<TicketSendingLine>();

    // Stock movements generated from this ticket
    public ICollection<StockMovementSending> StockMovements { get; set; } = new List<StockMovementSending>();

    // Audit
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public long CreatedByOperatorId { get; private set; }
    public long? UpdatedByOperatorId { get; private set; }

    private TicketSending() { }

    public TicketSending(
        long companyId,
        long siteId,
        long buyerId,
        string ticketNumber,
        string ticketType,
        decimal netWeightKg,
        decimal unitPricePerKg,
        string currencyCode,
        long createdByOperatorId,
        long? productId = null,
        string? productDescription = null,
        decimal? firstWeightKg = null,
        decimal? secondWeightKg = null,
        string? vehicleRegistration = null,
        string? trailerRegistration = null,
        string? driverName = null,
        string? notes = null)
    {
        CompanyId = companyId;
        SiteId = siteId;
        BuyerId = buyerId;
        TicketNumber = ticketNumber;
        TicketType = ticketType;
        NetWeightKg = netWeightKg;
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        UnitPricePerKg = unitPricePerKg;
        CurrencyCode = currencyCode;
        ProductId = productId;
        ProductDescription = productDescription;
        VehicleRegistration = vehicleRegistration;
        TrailerRegistration = trailerRegistration;
        DriverName = driverName;
        Notes = notes;
        CreatedByOperatorId = createdByOperatorId;

        TotalAmount = NetWeightKg * UnitPricePerKg;
    }

    public void UpdateWeights(decimal? firstWeightKg, decimal? secondWeightKg, decimal netWeightKg)
    {
        FirstWeightKg = firstWeightKg;
        SecondWeightKg = secondWeightKg;
        NetWeightKg = netWeightKg;
        TotalAmount = NetWeightKg * UnitPricePerKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void UpdatePrice(decimal unitPricePerKg)
    {
        UnitPricePerKg = unitPricePerKg;
        TotalAmount = NetWeightKg * UnitPricePerKg;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void UpdateDeliveryStatus(string status)
    {
        DeliveryStatus = status;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void UpdateRfidTag(string rfidTag, DateTimeOffset? firstScan = null, DateTimeOffset? secondScan = null)
    {
        RfidTag = rfidTag;
        if (firstScan.HasValue) RfidFirstScan = firstScan;
        if (secondScan.HasValue) RfidSecondScan = secondScan;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void AddLine(TicketSendingLine line)
    {
        Lines.Add(line);
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void SetPhotos(string? platePhotoUrl, string? loadPhotoUrl)
    {
        PlatePhotoUrl = platePhotoUrl;
        LoadPhotoUrl = loadPhotoUrl;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (!IsActive) return;
        
        IsActive = false;
        UpdatedTime = now;
    }
}
