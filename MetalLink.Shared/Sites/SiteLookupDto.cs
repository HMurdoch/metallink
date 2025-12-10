// File: MetalLink.Shared/Sites/SiteLookupDto.cs
namespace MetalLink.Shared.Sites;

public sealed class SiteLookupDto
{
    public long SiteId    { get; set; }
    public long CompanyId { get; set; }

    public string SiteName { get; set; } = string.Empty;
    public string? SiteCode { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }

    public int? ProvinceId   { get; set; }
    public string? ProvinceName { get; set; }
}
