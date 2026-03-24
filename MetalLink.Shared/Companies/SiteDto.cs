public sealed class SiteDto
{
    public long SiteId { get; set; }
    public long CompanyId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb       { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }
    public int? ProvinceId      { get; set; }
    public int? CountryId      { get; set; }
    public string? ProvinceName { get; set; }
    public bool IsActive { get; set; } = false;

    public string? CipcDocumentPath { get; set; }
    public string? TradingLicensePath { get; set; }
    public string? VatRegistrationCertificatePath { get; set; }
    public string? TaxClearanceCertificatePath { get; set; }
    public string? BbbeeComplianceCertificatePath { get; set; }
}