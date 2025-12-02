namespace MetalLink.Shared.Customers;

public sealed class CustomerDto
{
    public long CustomerId { get; set; }
    public long SiteId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public bool IsCompany { get; set; }
    public string? CompanyName { get; set; }

    public string? IdNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? PriceCode { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
}
