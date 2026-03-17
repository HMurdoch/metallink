// File: MetalLink.Shared/Sites/SiteLookupDto.cs
namespace MetalLink.Shared.Sites;

public class SiteLookupDto
{
    public int SiteId    { get; set; }
    public int? CompanyId { get; set; }

    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }

    public int? ProvinceId   { get; set; }
    public int? CountryId { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsDeleteVisible { get; set; } = true;
}
