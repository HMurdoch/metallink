using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Country
{
    public int CountryId { get; set; }

    public string? CountryCode { get; set; }
    public string CountryName { get; set; } = string.Empty;

    public int CreatedByOperatorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }

    public ICollection<Site> Sites { get; set; } = new List<Site>();
}
