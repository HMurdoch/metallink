public sealed class CompanyDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? VatNumber  { get; set; }
    public bool IsActive { get; set; } = true;
}
