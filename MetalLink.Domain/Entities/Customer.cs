using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalLink.Domain.Entities;

public class Customer
{
    public long CustomerId { get; set; }

    // Company
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // Site
    public long SiteId { get; set; }           // DB is NOT NULL, but we keep this optional in code for now
    public Site? Site { get; set; }

    // Names
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }

    // Convenience – not mapped to a column
    public string FullName =>
        string.Join(" ", new[] { FirstName, LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    public bool IsCompany { get; set; }

    // Identifiers / pricing
    public string? IdNumber      { get; set; }
    public long? AccountNumber { get; set; }
    public string? PriceCode     { get; set; }

    // Contact
    public string? PhoneNumber   { get; set; }
    public string? MobileNumber  { get; set; }
    public string? Email         { get; set; }

    // NEW: moved from Company → Customer (matches DB)
    public bool Taxable { get; set; } = true;

    // Audit
    public bool     IsActive    { get; set; } = true;
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    // Navigations
    public ICollection<CustomerDocument> Documents { get; set; } = new List<CustomerDocument>();
    public ICollection<Ticket> Tickets            { get; set; } = new List<Ticket>();
}
