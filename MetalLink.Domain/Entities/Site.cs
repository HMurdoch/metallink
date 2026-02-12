using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Site
{
    public int SiteId { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? ProvinceId { get; set; }
    public Province? Province { get; set; }

    public int? CountryId { get; set; }
    public Country? Country { get; set; }

    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb      { get; set; }
    public string? City        { get; set; }
    public string? PostalCode  { get; set; }

    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
