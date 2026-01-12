using System;
using System.Text.Json.Serialization;

namespace MetalLink.Shared.Customers;

public sealed class CustomerDto
{
    // Core IDs
    public long  CustomerId { get; set; }
    public long?  CompanyId  { get; set; }

    // NOTE: SiteId is int? because we cast long → int in query handler
    public long?  SiteId     { get; set; }

    // Names
    public string? FullName  { get; set; }
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool   IsCompany  { get; set; }

    // Company
    public string? CompanyName { get; set; }
    public string? VatNumber   { get; set; }

    // Tax – now on Customer (not Company)
    public bool Taxable { get; set; }

    // Site (address belongs to Site, not Customer)
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;

    // Identity / account
    public string? IdNumber      { get; set; }
    
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? AccountNumber { get; set; }

    public string AccountNumberFormatted
    => AccountNumber.HasValue
        ? AccountNumber.Value.ToString("D8")
        : "";
    
    public string AccountNumberDisplay => AccountNumber.HasValue
    ? AccountNumber.Value.ToString("D8")
    : string.Empty;
    public string? PriceCode     { get; set; }

    // Contact
    public string? PhoneNumber  { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email        { get; set; }

    // Status
    public bool     IsActive    { get; set; }
    public DateTime? CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }

    // Customer Images (file storage paths)
    public string? IdCardImagePath { get; set; }
    public string? DriverLicenseImagePath { get; set; }
    public string? PhotoImagePath { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? FingerprintImagePath { get; set; }
}
