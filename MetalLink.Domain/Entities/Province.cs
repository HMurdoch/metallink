using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Province
{
    public int ProvinceId { get; set; }

    public string ProvinceName { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public ICollection<Site> Sites { get; set; } = new List<Site>();
}
