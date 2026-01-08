using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Company
{
    public long CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string? VatNumber  { get; set; }

    public bool     IsActive    { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    public ICollection<Site>     Sites     { get; set; } = new List<Site>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
