using System;
using System.Text.Json.Serialization;
using MetalLink.Shared.Json;

namespace MetalLink.Shared.Customers;

public sealed class CustomerDto
{
    // Core IDs
    public int CustomerId { get; set; }
    public int? CompanyId  { get; set; }
    public int? SiteId     { get; set; }

    // Names
    public string? FullName  { get; set; }
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool   IsCompany  { get; set; }

    // Company
    public string? CompanyName { get; set; }
    public string? VatNumber   { get; set; }

    // Tax – now on Customer
    public bool IsTaxable { get; set; }
    public bool Taxable { get; set; }

    // Site (address belongs to Site, not Customer)
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;

    // Identity / account
    public string? IdNumber      { get; set; }
    
    [JsonConverter(typeof(PaddedAccountNumberLongConverter))]
    public long? AccountNumber { get; set; }

    public string AccountNumberFormatted
    => AccountNumber.HasValue
        ? AccountNumber.Value.ToString("D8")
        : "";
    
    public string AccountNumberDisplay => AccountNumber.HasValue
    ? AccountNumber.Value.ToString("D8")
    : string.Empty;
    public string? PriceCode     { get; set; }
    public int? ProductPriceListId { get; set; }
    public string? ProductPriceListName { get; set; }

    // Contact
    public string? PhoneNumber  { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email        { get; set; }

    // Status
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    // Image paths relationship
    public int? ImagePathId { get; set; }
    public string? IdCardImagePath { get; set; }
    public string? DriverLicenseImagePath { get; set; }
    public string? PhotoImagePath { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? FingerprintImagePath { get; set; }

    public byte[]? IdCardImage { get; set; }
    public byte[]? DriverLicenseImage { get; set; }
    public byte[]? PhotoImage { get; set; }
    public byte[]? SignatureImage { get; set; }
    public byte[]? FingerprintImage { get; set; }
}
