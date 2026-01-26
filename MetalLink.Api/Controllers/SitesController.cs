using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Application.Services;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Sites;
using MetalLink.Api.Extensions;

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
        [FromQuery] int companyId,
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

        var company = await _db.Companies.Include(c => c.Sites).FirstOrDefaultAsync(c => c.CompanyId == dto.CompanyId, ct);
        if (company == null)
            return BadRequest($"Company {dto.CompanyId} not found.");

        // Generate site code if not provided
        var siteCode = dto.SiteCode;
        if (string.IsNullOrWhiteSpace(siteCode))
        {
            siteCode = SiteCodeGeneratorService.GenerateNextSiteCode(company.Sites);
        }

        var entity = new Site
        {
            CompanyId = dto.CompanyId,
            SiteName = name,
            IsActive = dto.IsActive,
            CreatedByOperatorId = (int)User.GetOperatorId(),

            SiteCode = siteCode,
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
    [HttpPut("{siteId:int}")]
    public async Task<IActionResult> Update(int siteId, [FromBody] SiteLookupDto dto, CancellationToken ct)
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
        site.ProvinceId = dto.ProvinceId ?? site.ProvinceId;
        site.CountryId = dto.CountryId ?? site.CountryId;
        site.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/sites/{siteId} (soft delete)
    [HttpDelete("{siteId:int}")]
    public async Task<IActionResult> Delete(int siteId, CancellationToken ct)
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
