namespace MetalLink.Shared.Provinces;

public sealed class ProvinceCreateRequestDto
{
    public string ProvinceName { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }

    public bool IsActive { get; set; } = true;
}
