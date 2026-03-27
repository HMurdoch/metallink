using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Products;
using MetalLink.Domain.Entities;
using MetalLink.Api.Extensions;

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
                p.IsriProductName.ToLower().Contains(searchTerm) ||
                p.IsriProductCode.ToLower().Contains(searchTerm) ||
                (p.StarredProductAlias != null && p.StarredProductAlias.ToLower().Contains(searchTerm)));
        }

        var results = await query
            .OrderBy(p => p.IsriProductName)
            .Select(p => new ProductLookupDto
            {
                ProductId = p.ProductId,
                ProductName = p.StarredProductAlias ?? p.IsriProductName,
                ProductCode = p.IsriProductCode,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return Ok(results);
    }

    // GET /api/products/{productId}
    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int productId, CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound();

        var dto = new ProductDto
        {
            ProductId = product.ProductId,
            HtsCode = product.HtsCode,
            IsriProductCode = product.IsriProductCode,
            IsriProductName = product.IsriProductName,
            IsriProductDescription = product.IsriProductDescription,
            IsriProductUrl = product.IsriProductUrl,
            IsriProduct = product.IsriProduct,
            ProductGroupId = product.ProductGroupId,
            ProductSpecificationFlagId = product.ProductSpecificationFlagId,
            StarredProduct = product.StarredProduct,
            StarredProductAlias = product.StarredProductAlias,
            MustDeclare = product.MustDeclare,
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
            Grade = dto.Grade,
            IsActive = true,
            CreatedByOperatorId = (int)User.GetOperatorId(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        // Legacy price record creation removed - prices are now in product_price_list_product_prices table

        dto.ProductId = product.ProductId;
        dto.CreatedTime = product.CreatedTime;
        dto.UpdatedTime = product.UpdatedTime;

        return CreatedAtAction(nameof(GetById), new { productId = product.ProductId }, dto);
    }

    // PUT /api/products/{productId}
    [HttpPut("{productId:int}")]
    public async Task<IActionResult> Update(
        int productId,
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
    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Delete(int productId, CancellationToken ct)
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
