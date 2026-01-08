namespace MetalLink.Shared.Companies;

public sealed class CompanyCreateDto
{
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? VatNumber { get; set; }
}
