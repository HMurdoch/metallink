namespace MetalLink.Shared.Provinces;

public sealed class ProvinceSearchRequestDto
{
    /// <summary>
    /// Free-text filter (optional). If null or empty, returns all provinces.
    /// </summary>
    public string? Query { get; set; }
}
