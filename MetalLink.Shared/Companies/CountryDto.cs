public sealed class CountryDto
{
    public int? CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    
    // Status
    public bool   IsActive    { get; set; }
    public DateTime? CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}