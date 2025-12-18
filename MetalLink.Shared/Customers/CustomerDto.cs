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

    // Site / address (all from Site, NOT Customer)
    public string? SiteName     { get; set; }
    public string? SiteCode     { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }

    // Province / Country
    public long?    ProvinceId   { get; set; }
    public string? ProvinceName { get; set; }

    public long?    CountryId    { get; set; }
    public string? CountryName  { get; set; }

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
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
