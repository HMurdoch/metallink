using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Country
{
    public int CountryId { get; set; }

    public string  Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public bool     IsActive    { get; set; } = true;
    public DateTime? CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }

    public ICollection<Site> Sites { get; set; } = new List<Site>();
}
