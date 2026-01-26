using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Companies;
using MetalLink.Api.Extensions;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public CompaniesController(MetalLinkDbContext db)
    {
        _db = db;
    }

    // GET api/companies/lookup?term=foo
    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyList<CompanyLookupDto>>> Lookup([FromQuery] string? term, CancellationToken ct)
    {
        var q = _db.Companies.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            q = q.Where(c => c.CompanyName.Contains(t));
        }

        var items = await q
            .OrderBy(c => c.CompanyName)
            .Select(c => new CompanyLookupDto
            {
                CompanyId = c.CompanyId,
                CompanyName = c.CompanyName,
                VatNumber = c.VatNumber,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // POST api/companies
    [HttpPost]
    public async Task<ActionResult<CompanyLookupDto>> Create([FromBody] CompanyCreateDto dto, CancellationToken ct)
    {
        if (dto == null)
            return BadRequest("Body required.");

        var name = (dto.CompanyName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("CompanyName is required.");

        var entity = new Company
        {
            CompanyName = name,
            VatNumber = dto.VatNumber,
            IsActive = dto.IsActive,
            CreatedByOperatorId = (int)User.GetOperatorId(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.Companies.Add(entity);
        await _db.SaveChangesAsync(ct);

        var result = new CompanyLookupDto
        {
            CompanyId = entity.CompanyId,
            CompanyName = entity.CompanyName,
            VatNumber = entity.VatNumber,
            IsActive = entity.IsActive,
        };

        // Returns 201 + Location header
        return CreatedAtAction(nameof(GetById), new { companyId = entity.CompanyId }, result);
    }

    // GET api/companies/{companyId}
    [HttpGet("{companyId:long}")]
    public async Task<ActionResult<CompanyLookupDto>> GetById(long companyId, CancellationToken ct)
    {
        var c = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);
        if (c == null) return NotFound();

        return Ok(new CompanyLookupDto
        {
            CompanyId = c.CompanyId,
            CompanyName = c.CompanyName,
            VatNumber = c.VatNumber,
            IsActive = c.IsActive
        });
    }

    // PUT /api/companies/14
    [HttpPut("{companyId:long}")]
    public async Task<IActionResult> Update(long companyId, [FromBody] CompanyDto dto, CancellationToken ct)
    {
        if (dto == null) return BadRequest("Body required.");

        var company = await _db.Companies.FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);
        if (company == null) return NotFound();

        var name = (dto.CompanyName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("CompanyName is required.");

        company.CompanyName = name;
        company.VatNumber = string.IsNullOrWhiteSpace(dto.VatNumber) ? null : dto.VatNumber.Trim();
        company.IsActive = dto.IsActive;

        // IMPORTANT: fix -infinity if you want (recommended)
        company.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/companies/14  (soft delete)
    [HttpDelete("{companyId:long}")]
    public async Task<IActionResult> Delete(long companyId, CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);
        if (company == null) return NotFound();

        company.IsActive = false;
        company.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
