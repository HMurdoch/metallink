public sealed class SiteDto
{
    public long SiteId { get; set; }
    public long CompanyId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteCode { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }
    public int? ProvinceId      { get; set; }
    public string? ProvinceName { get; set; }
    public bool IsActive { get; set; } = false;
}