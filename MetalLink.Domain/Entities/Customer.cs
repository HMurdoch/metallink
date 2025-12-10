using System;

namespace MetalLink.Domain.Entities;

public class Customer
{
    public long CustomerId { get; set; }

    public long CompanyId { get; set; }      // later we can make this long? if you want truly optional
    public Company Company { get; set; } = null!;

    public long? SiteId { get; set; }        // optional: which branch/site
    public Site? Site { get; set; }

    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsCompany { get; set; }

    public string? IdNumber      { get; set; }
    public string? AccountNumber { get; set; }
    public string? PriceCode     { get; set; }

    public string? PhoneNumber   { get; set; }
    public string? MobileNumber  { get; set; }
    public string? Email         { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}
