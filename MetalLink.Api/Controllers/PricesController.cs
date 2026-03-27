using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Prices;
using MetalLink.Domain.Entities;
using MetalLink.Api.Extensions;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/prices")]
public class PricesController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public PricesController(MetalLinkDbContext db)
    {
        _db = db;
    }

    // GET /api/prices/product/{productId} - Get single legacyPrice for a product
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<PriceDto>> GetByProductId(int productId, CancellationToken ct)
    {
        var legacyPrice = await _db.LegacyPrices
            .Where(p => p.ProductId == productId && p.IsActive)
            .FirstOrDefaultAsync(ct);

        if (legacyPrice == null)
            return NotFound();

        var dto = new PriceDto
        {
            PriceId = legacyPrice.LegacyPriceId,
            ProductId = legacyPrice.ProductId,
            IsActive = legacyPrice.IsActive,
            CreatedTime = legacyPrice.CreatedTime,
            UpdatedTime = legacyPrice.UpdatedTime
        };

        return Ok(dto);
    }

    // GET /api/prices/{legacyPriceId}
    [HttpGet("{legacyPriceId:int}")]
    public async Task<ActionResult<PriceDto>> GetById(int legacyPriceId, CancellationToken ct)
    {
        var legacyPrice = await _db.LegacyPrices
            .FirstOrDefaultAsync(p => p.LegacyPriceId == legacyPriceId, ct);

        if (legacyPrice == null)
            return NotFound();

        var dto = new PriceDto
        {
            PriceId = legacyPrice.LegacyPriceId,
            ProductId = legacyPrice.ProductId,
            IsActive = legacyPrice.IsActive,
            CreatedTime = legacyPrice.CreatedTime,
            UpdatedTime = legacyPrice.UpdatedTime
        };

        return Ok(dto);
    }

    // POST /api/prices
    [HttpPost]
    public async Task<ActionResult<PriceDto>> Create(
        [FromBody] PriceDto dto,
        CancellationToken ct)
    {
        var legacyPrice = new LegacyPrice
        {
            ProductId = dto.ProductId,
            IsActive = true,
            CreatedByOperatorId = (int)User.GetOperatorId(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.LegacyPrices.Add(legacyPrice);
        await _db.SaveChangesAsync(ct);

        dto.PriceId = legacyPrice.LegacyPriceId;
        dto.CreatedTime = legacyPrice.CreatedTime;
        dto.UpdatedTime = legacyPrice.UpdatedTime;

        return CreatedAtAction(nameof(GetById), new { legacyPriceId = legacyPrice.LegacyPriceId }, dto);
    }

    // PUT /api/prices/{legacyPriceId}
    [HttpPut("{legacyPriceId:int}")]
    public async Task<IActionResult> Update(
        int legacyPriceId,
        [FromBody] PriceDto dto,
        CancellationToken ct)
    {
        var legacyPrice = await _db.LegacyPrices.FirstOrDefaultAsync(p => p.LegacyPriceId == legacyPriceId, ct);
        if (legacyPrice == null)
            return NotFound();

        legacyPrice.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/prices/{legacyPriceId} (soft delete)
    [HttpDelete("{legacyPriceId:int}")]
    public async Task<IActionResult> Delete(int legacyPriceId, CancellationToken ct)
    {
        var legacyPrice = await _db.LegacyPrices.FirstOrDefaultAsync(p => p.LegacyPriceId == legacyPriceId, ct);
        if (legacyPrice == null)
            return NotFound();

        if (!legacyPrice.IsActive)
            return BadRequest("LegacyPrice is already inactive.");

        legacyPrice.IsActive = false;
        legacyPrice.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
