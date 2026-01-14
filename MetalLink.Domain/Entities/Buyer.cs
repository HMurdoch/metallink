using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

/// <summary>
/// Represents a buyer/merchant that purchases scrap metal from us (outbound/sending).
/// Similar to Customer, but for the opposite flow.
/// </summary>
public class Buyer
{
    public long BuyerId { get; set; }

    // Company details
    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    public long? SiteId { get; set; }
    public Site? Site { get; set; }

    // Buyer details
    public string? BuyerName { get; set; }
    public string? ContactPerson { get; set; }
    public bool IsCompany { get; set; } = true;

    // Identifiers
    public string? RegistrationNumber { get; set; }  // Company registration
    public string? VatNumber { get; set; }
    public long? AccountNumber { get; set; }
    public string? PriceCode { get; set; }

    // Contact information
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // Business details
    public bool Taxable { get; set; } = true;
    public string? PaymentTerms { get; set; }  // e.g., "30 days", "COD"
    public string? Notes { get; set; }

    // Audit fields
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? CreatedTime { get; set; }
    public DateTimeOffset? UpdatedTime { get; set; }

    // Navigation
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
