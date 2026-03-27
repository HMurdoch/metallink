using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Products;
using MetalLink.Shared.Sites;
using MetalLink.Shared.Tickets.Receiving;

namespace MetalLink.Desktop.Properties;

/// <summary>
/// Properties for Ticket Receiving operations (buying from customers/suppliers)
/// </summary>
public sealed class TicketReceivingProperties
{
    // Customer/Supplier related
    public CustomerDto? SelectedCustomer { get; set; }
    public ObservableCollection<CustomerDto> CustomerSuggestions { get; set; } = new();
    public string CustomerSearchText { get; set; } = string.Empty;

    // Ticket Header
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketCustomerIdText { get; set; } = string.Empty;
    public string TicketType { get; set; } = "weighbridge"; // weighbridge or platform
    
    // Weights
    public decimal? FirstWeightKg { get; set; }
    public decimal? SecondWeightKg { get; set; }
    public decimal NetWeightKg { get; set; }
    
    // Pricing
    public decimal UnitPricePerKg { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    
    // Product (for weighbridge tickets)
    public ProductLookupDto? SelectedProduct { get; set; }
    public ObservableCollection<ProductLookupDto> ProductSuggestions { get; set; } = new();
    public string? ProductDescription { get; set; }
    
    // Vehicle Details
    public string? VehicleRegistration { get; set; }
    public string? TrailerRegistration { get; set; }
    public string? DriverName { get; set; }
    
    // Reference Numbers
    public string? OfmWeighbridgeTicket { get; set; }
    public string? ForeignTicket { get; set; }
    public string? CkNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    
    // RFID
    public string? RfidTag { get; set; }
    public DateTimeOffset? RfidFirstScan { get; set; }
    public DateTimeOffset? RfidSecondScan { get; set; }
    
    // Status
    public string DeliveryStatus { get; set; } = "pending";
    public string? Notes { get; set; }
    
    // Photos
    public string? PlatePhotoUrl { get; set; }
    public string? LoadPhotoUrl { get; set; }
    
    // Platform Ticket Lines (for multi-product tickets)
    public ObservableCollection<TicketReceivingLineDto> Lines { get; set; } = new();
    
    // Calculated Totals
    public decimal SubTotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public decimal VatRate { get; set; } = 0.15m; // 15% VAT
    
    // Site and Company
    public SiteLookupDto? SelectedSite { get; set; }
    public int CompanyId { get; set; }
    public int SiteId { get; set; }
    
    // Operator
    public int OperatorId { get; set; }
    
    // Created ticket info
    public int? TicketReceivingId { get; set; }
    public DateTimeOffset? CreatedTime { get; set; }
    public DateTimeOffset? UpdatedTime { get; set; }
}
