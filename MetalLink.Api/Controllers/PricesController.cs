using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Prices;
using MetalLink.Domain.Entities;

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

    // GET /api/prices/product/{productId} - Get single price for a product
    [HttpGet("product/{productId:long}")]
    public async Task<ActionResult<PriceDto>> GetByProductId(long productId, CancellationToken ct)
    {
        var price = await _db.Prices
            .Where(p => p.ProductId == productId && p.IsActive)
            .FirstOrDefaultAsync(ct);

        if (price == null)
            return NotFound();

        var dto = new PriceDto
        {
            PriceId = price.PriceId,
            ProductId = price.ProductId,
            PriceA = price.PriceA,
            PriceB = price.PriceB,
            PriceC = price.PriceC,
            IsActive = price.IsActive,
            CreatedTime = price.CreatedTime,
            UpdatedTime = price.UpdatedTime
        };

        return Ok(dto);
    }

    // GET /api/prices/{priceId}
    [HttpGet("{priceId:long}")]
    public async Task<ActionResult<PriceDto>> GetById(long priceId, CancellationToken ct)
    {
        var price = await _db.Prices
            .FirstOrDefaultAsync(p => p.PriceId == priceId, ct);

        if (price == null)
            return NotFound();

        var dto = new PriceDto
        {
            PriceId = price.PriceId,
            ProductId = price.ProductId,
            PriceA = price.PriceA,
            PriceB = price.PriceB,
            PriceC = price.PriceC,
            IsActive = price.IsActive,
            CreatedTime = price.CreatedTime,
            UpdatedTime = price.UpdatedTime
        };

        return Ok(dto);
    }

    // POST /api/prices
    [HttpPost]
    public async Task<ActionResult<PriceDto>> Create(
        [FromBody] PriceDto dto,
        CancellationToken ct)
    {
        var price = new Price
        {
            ProductId = dto.ProductId,
            PriceA = dto.PriceA,
            PriceB = dto.PriceB,
            PriceC = dto.PriceC,
            IsActive = true,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.Prices.Add(price);
        await _db.SaveChangesAsync(ct);

        dto.PriceId = price.PriceId;
        dto.CreatedTime = price.CreatedTime;
        dto.UpdatedTime = price.UpdatedTime;

        return CreatedAtAction(nameof(GetById), new { priceId = price.PriceId }, dto);
    }

    // PUT /api/prices/{priceId}
    [HttpPut("{priceId:long}")]
    public async Task<IActionResult> Update(
        long priceId,
        [FromBody] PriceDto dto,
        CancellationToken ct)
    {
        var price = await _db.Prices.FirstOrDefaultAsync(p => p.PriceId == priceId, ct);
        if (price == null)
            return NotFound();

        price.PriceA = dto.PriceA;
        price.PriceB = dto.PriceB;
        price.PriceC = dto.PriceC;
        price.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/prices/{priceId} (soft delete)
    [HttpDelete("{priceId:long}")]
    public async Task<IActionResult> Delete(long priceId, CancellationToken ct)
    {
        var price = await _db.Prices.FirstOrDefaultAsync(p => p.PriceId == priceId, ct);
        if (price == null)
            return NotFound();

        if (!price.IsActive)
            return BadRequest("Price is already inactive.");

        price.IsActive = false;
        price.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
