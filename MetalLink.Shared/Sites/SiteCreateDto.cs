namespace MetalLink.Shared.Sites;

public sealed class SiteCreateDto
{
    public long CompanyId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Optional fields (only if your DB/entity supports them)
    public string? SiteCode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public long? ProvinceId { get; set; }
    public int? CountryId { get; set; }
}
