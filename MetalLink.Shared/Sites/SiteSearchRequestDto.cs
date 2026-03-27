namespace MetalLink.Shared.Sites;

public class SiteSearchRequestDto
{
    public int? CompanyId { get; set; }
    public string? Term { get; set; } = "";
    public bool? IsActive { get; set; } = true;
}
