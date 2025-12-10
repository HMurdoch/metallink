namespace MetalLink.Shared.Customers;

public sealed class CustomerDto
{
    // Core IDs
    public long CustomerId { get; set; }

    public long CompanyId  { get; set; }
    public long? SiteId    { get; set; }

    // Names
    public string? FullName  { get; set; }   // From entity expression
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool   IsCompany  { get; set; }

    // Company
    public string? CompanyName { get; set; }
    public string? VatNumber   { get; set; }
    public bool   Taxable      { get; set; }

    // Site
    public string? SiteName  { get; set; }
    public string? SiteCode  { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }

    // Province
    public int?    ProvinceId   { get; set; }
    public string? ProvinceName { get; set; }

    // Identity / account
    public string? IdNumber      { get; set; }
    public string? AccountNumber { get; set; }
    public string? PriceCode     { get; set; }

    // Contact
    public string? PhoneNumber  { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email        { get; set; }

    // Status
    public bool   IsActive    { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
