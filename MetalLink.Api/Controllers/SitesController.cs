using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Sites;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/sites")]
public sealed class SitesController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public SitesController(MetalLinkDbContext db)
    {
        _db = db;
    }

    // GET /api/sites/lookup?companyId=8&term=
    [HttpGet("lookup")]
    public async Task<ActionResult<List<SiteLookupDto>>> Lookup(
        [FromQuery] long companyId,
        [FromQuery] string? term = null,
        CancellationToken ct = default)
    {
        term ??= "";

        var query = _db.Sites.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(s => EF.Functions.ILike(s.SiteName!, $"%{t}%"));
        }

        var results = await query
            .OrderBy(s => s.SiteName)
            .Select(s => new SiteLookupDto
            {
                SiteId = s.SiteId,
                CompanyId = s.CompanyId,
                SiteName = s.SiteName,
                ProvinceId = s.ProvinceId,
                CountryId = s.CountryId,
                IsActive = s.IsActive,

                SiteCode = s.SiteCode,
                AddressLine1 = s.AddressLine1,
                AddressLine2 = s.AddressLine2,
                Suburb = s.Suburb,
                City = s.City,
                PostalCode = s.PostalCode
            })
            .ToListAsync(ct);

        return Ok(results);
    }

    // POST /api/sites
    [HttpPost]
    public async Task<ActionResult<SiteLookupDto>> Create([FromBody] SiteCreateDto dto, CancellationToken ct)
    {
        if (dto == null)
            return BadRequest("Body required.");

        var name = (dto.SiteName ?? string.Empty).Trim();
        if (dto.CompanyId <= 0)
            return BadRequest("CompanyId is required.");
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("SiteName is required.");

        var companyExists = await _db.Companies.AnyAsync(c => c.CompanyId == dto.CompanyId, ct);
        if (!companyExists)
            return BadRequest($"Company {dto.CompanyId} not found.");

        // if this site is being created as the first site for a company
        if (string.IsNullOrWhiteSpace(dto.SiteCode))
            dto.SiteCode = "SITE-1";

        var entity = new Site
        {
            CompanyId = dto.CompanyId,
            SiteName = name,
            IsActive = dto.IsActive,

            SiteCode = dto.SiteCode,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            Suburb = dto.Suburb,
            City = dto.City,
            PostalCode = dto.PostalCode,
            ProvinceId = dto.ProvinceId,
            CountryId = dto.CountryId
        };

        _db.Sites.Add(entity);
        await _db.SaveChangesAsync(ct);

        var result = new SiteLookupDto
        {
            SiteId = entity.SiteId,
            CompanyId = entity.CompanyId,
            SiteName = entity.SiteName,
            SiteCode = entity.SiteCode,
            AddressLine1 = entity.AddressLine1,
            AddressLine2 = entity.AddressLine2,
            Suburb = entity.Suburb,
            City = entity.City,
            PostalCode = entity.PostalCode,
            ProvinceId = entity.ProvinceId,
            CountryId = entity.CountryId,
            IsActive = entity.IsActive
        };

        return Created($"api/sites/{entity.SiteId}", result);
    }

    // PUT /api/sites/{siteId}
    [HttpPut("{siteId:long}")]
    public async Task<IActionResult> Update(long siteId, [FromBody] SiteLookupDto dto, CancellationToken ct)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site == null) return NotFound();

        site.SiteName = dto.SiteName ?? site.SiteName;
        site.SiteCode = dto.SiteCode;
        site.AddressLine1 = dto.AddressLine1;
        site.AddressLine2 = dto.AddressLine2;
        site.Suburb = dto.Suburb;
        site.City = dto.City;
        site.PostalCode = dto.PostalCode;
        site.ProvinceId = dto.ProvinceId;
        site.CountryId = dto.CountryId;
        site.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/sites/{siteId} (soft delete)
    [HttpDelete("{siteId:long}")]
    public async Task<IActionResult> Delete(long siteId, CancellationToken ct)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site == null) return NotFound();
        
        if (!site.IsActive)
        {
            return BadRequest("Site is already inactive.");
        }

        // Validation: Count total active sites for this company (including current one)
        var totalActiveSites = await _db.Sites
            .Where(s => s.CompanyId == site.CompanyId && s.IsActive)
            .CountAsync(ct);

        if (totalActiveSites <= 1)
        {
            return BadRequest("Cannot delete the last active site. A company must have at least one active site.");
        }

        site.IsActive = false;
        site.UpdatedTime = DateTimeOffset.UtcNow;
        
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
