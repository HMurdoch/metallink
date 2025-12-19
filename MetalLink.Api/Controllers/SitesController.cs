using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Sites;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public SitesController(MetalLinkDbContext db)
    {
        _db = db;
    }

    // PUT api/sites/{siteId}
    [HttpPut("{siteId:long}")]
    public async Task<IActionResult> UpdateSite(
        long siteId,
        [FromBody] SiteLookupDto dto,
        CancellationToken ct)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site == null)
            return NotFound();

        site.SiteName = dto.SiteName;
        site.SiteCode = dto.SiteCode;
        site.AddressLine1 = dto.AddressLine1;
        site.AddressLine2 = dto.AddressLine2;
        site.Suburb = dto.Suburb;
        site.City = dto.City;
        site.PostalCode = dto.PostalCode;
        site.ProvinceId = dto.ProvinceId;
        site.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE api/sites/{siteId}
    [HttpDelete("{siteId:long}")]
    public async Task<IActionResult> SoftDeleteSite(long siteId, CancellationToken ct)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site == null)
            return NotFound();

        site.IsActive = false;
        site.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
