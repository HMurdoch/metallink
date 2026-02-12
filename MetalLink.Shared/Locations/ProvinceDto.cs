// File: MetalLink.Shared/Locations/ProvinceDto.cs
namespace MetalLink.Shared.Locations;

public sealed class ProvinceDto
{
    public int    ProvinceId   { get; set; }
    public string ProvinceName { get; set; } = string.Empty;

    public bool   IsActive    { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
