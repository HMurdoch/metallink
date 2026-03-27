namespace MetalLink.Shared.Customers;

public sealed class CustomerSearchRequestDto
{
    public int? CustomerId { get; set; }
    public int? SiteId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName  { get; set; }

    public string? CompanyName   { get; set; }
    public string? IdNumber      { get; set; }
    public long? AccountNumber { get; set; }
    public int? ProductPriceListId { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }

    public string? PhoneNumber  { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email        { get; set; }

    // NEW – extra filters
    public int? ProvinceId { get; set; }
    public int? CountryId { get; set; }
    public bool? Taxable    { get; set; }
}
