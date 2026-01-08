namespace MetalLink.Shared.Sites;

public class SiteSearchRequestDto
{
    public long? CompanyId { get; set; }
    public string? Term { get; set; } = "";
    public bool? IsActive { get; set; } = true;
}
