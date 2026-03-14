using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalLink.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string? IdNumber { get; set; }

    /// <summary>
    /// Globally unique across customers and buyers.
    /// Stored as numeric; UI/API can format as D8.
    /// </summary>
    public long? AccountNumber { get; set; }

    public bool IsCompany { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? SiteId { get; set; }
    public Site? Site { get; set; }

    public bool IsTaxable { get; set; }

    public int? ProductPriceListId { get; set; }
    public ProductPriceList? ProductPriceList { get; set; }

    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }

    public int? ImagePathId { get; set; }
    public ImagePath? ImagePath { get; set; }

    public int CreatedByOperatorId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    // Navigation (legacy - kept to avoid breaking compilation where referenced)
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
