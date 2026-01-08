using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Site
{
    public long SiteId { get; set; }

    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int? ProvinceId { get; set; }
    public Province? Province { get; set; } = null!;

    public int? CountryId { get; set; }
    public Country? Country { get; set; } = null!;

    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb      { get; set; }
    public string? City        { get; set; }
    public string? PostalCode  { get; set; }

    public bool     IsActive    { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
