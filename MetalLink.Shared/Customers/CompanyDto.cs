public sealed class CompanyDto
{
    public long   CompanyId   { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? VatNumber  { get; set; }
}
