using System;
using System.Collections.Generic;
using System.Linq;

namespace MetalLink.Domain.Entities;

public class Buyer
{
    public int BuyerId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string? IdNumber { get; set; }
    public long? AccountNumber { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;

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

    // Navigation to TicketSending
    public ICollection<TicketSending> TicketsSending { get; set; } = new List<TicketSending>();
}
