namespace MetalLink.Shared.Provinces;

public sealed class ProvinceDto
{
    public int? ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}
