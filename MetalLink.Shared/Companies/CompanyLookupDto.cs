// File: MetalLink.Shared/Companies/CompanyLookupDto.cs
namespace MetalLink.Shared.Companies;

public sealed class CompanyLookupDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public string? VatNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
