using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Products;
using MetalLink.Domain.Entities;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public ProductsController(MetalLinkDbContext db)
    {
        _db = db;
    }

    // GET /api/products/lookup?term=abc
    [HttpGet("lookup")]
    public async Task<ActionResult<IEnumerable<ProductLookupDto>>> Lookup(
        [FromQuery] string? term,
        CancellationToken ct)
    {
        // Only return active products
        var query = _db.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var searchTerm = term.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(searchTerm) ||
                (p.ProductCode != null && p.ProductCode.ToLower().Contains(searchTerm)));
        }

        var results = await query
            .OrderBy(p => p.ProductName)
            .Select(p => new ProductLookupDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ProductCode = p.ProductCode,
                Grade = p.Grade,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return Ok(results);
    }

    // GET /api/products/{productId}
    [HttpGet("{productId:long}")]
    public async Task<ActionResult<ProductDto>> GetById(long productId, CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound();

        var dto = new ProductDto
        {
            ProductId = product.ProductId,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Grade = product.Grade,
            IsActive = product.IsActive,
            CreatedTime = product.CreatedTime,
            UpdatedTime = product.UpdatedTime
        };

        return Ok(dto);
    }

    // POST /api/products
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] ProductDto dto,
        CancellationToken ct)
    {
        var product = new Product
        {
            ProductCode = dto.ProductCode.Trim(),
            ProductName = dto.ProductName.Trim(),
            Grade = dto.Grade ?? 0,
            IsActive = true,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        // Automatically create a price record with default values
        var price = new Price
        {
            ProductId = product.ProductId,
            PriceA = 0,
            PriceB = 0,
            PriceC = 0,
            IsActive = true,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.Prices.Add(price);
        await _db.SaveChangesAsync(ct);

        dto.ProductId = product.ProductId;
        dto.CreatedTime = product.CreatedTime;
        dto.UpdatedTime = product.UpdatedTime;

        return CreatedAtAction(nameof(GetById), new { productId = product.ProductId }, dto);
    }

    // PUT /api/products/{productId}
    [HttpPut("{productId:long}")]
    public async Task<IActionResult> Update(
        long productId,
        [FromBody] ProductDto dto,
        CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product == null)
            return NotFound();

        product.ProductCode = dto.ProductCode.Trim();
        product.ProductName = dto.ProductName.Trim();
        product.Grade = dto.Grade;
        product.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/products/{productId} (soft delete)
    [HttpDelete("{productId:long}")]
    public async Task<IActionResult> Delete(long productId, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product == null)
            return NotFound();

        if (!product.IsActive)
            return BadRequest("Product is already inactive.");

        product.IsActive = false;
        product.UpdatedTime = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
