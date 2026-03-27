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
    [HttpGet("groups")]
    public async Task<ActionResult<IEnumerable<ProductGroupDto>>> GetGroups(CancellationToken ct)
    {
        var groups = await _db.ProductGroups
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProductGroupName)
            .Select(p => new ProductGroupDto { ProductGroupId = p.ProductGroupId, ProductGroupName = p.ProductGroupName })
            .ToListAsync(ct);
        return Ok(groups);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<IEnumerable<ProductLookupDto>>> Lookup(
        [FromQuery] string? term,
        [FromQuery] int? groupId,
        [FromQuery] string? letter,
        [FromQuery] bool includeNonStarred = false,
        CancellationToken ct = default)
    {
        var query = _db.Products
            .Include(p => p.ProductGroup)
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!includeNonStarred)
        {
            query = query.Where(p => p.StarredProduct);
        }

        if (groupId.HasValue && groupId.Value > 0)
        {
            query = query.Where(p => p.ProductGroupId == groupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(letter) && letter != "ALL")
        {
            query = query.Where(p => EF.Functions.ILike(p.StarredProductAlias ?? p.IsriProductName, $"{letter}%"));
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var searchTerm = term.Trim().ToLower();
            query = query.Where(p =>
                p.IsriProductName.ToLower().Contains(searchTerm) ||
                p.IsriProductCode.ToLower().Contains(searchTerm) ||
                (p.StarredProductAlias != null && p.StarredProductAlias.ToLower().Contains(searchTerm)));
        }

        var results = await query
            .OrderBy(p => p.StarredProductAlias ?? p.IsriProductName)
            .Select(p => new ProductLookupDto
            {
                ProductId = p.ProductId,
                ProductName = p.IsriProductName,
                ProductCode = p.IsriProductCode,
                HtsCode = p.HtsCode,
                IsriProduct = p.IsriProduct,
                ProductGroupName = p.ProductGroup != null ? p.ProductGroup.ProductGroupName : null,
                ProductSpecificationFlagId = p.ProductSpecificationFlagId,
                StarredProductAlias = p.StarredProductAlias,
                StarredProduct = p.StarredProduct,
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
            HtsCode = dto.HtsCode,
            IsriProductCode = dto.IsriProductCode.Trim(),
            IsriProductName = dto.IsriProductName.Trim(),
            IsriProductDescription = dto.IsriProductDescription,
            IsriProductUrl = dto.IsriProductUrl,
            IsriProduct = dto.IsriProduct,
            ProductGroupId = dto.ProductGroupId,
            ProductSpecificationFlagId = dto.ProductSpecificationFlagId,
            StarredProduct = dto.StarredProduct,
            StarredProductAlias = dto.StarredProductAlias,
            MustDeclare = dto.MustDeclare,
            IsActive = true,
            CreatedByOperatorId = (int)User.GetOperatorId(),
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

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

        product.HtsCode = dto.HtsCode;
        product.IsriProductCode = dto.IsriProductCode.Trim();
        product.IsriProductName = dto.IsriProductName.Trim();
        product.IsriProductDescription = dto.IsriProductDescription;
        product.IsriProductUrl = dto.IsriProductUrl;
        product.IsriProduct = dto.IsriProduct;
        product.ProductGroupId = dto.ProductGroupId;
        product.ProductSpecificationFlagId = dto.ProductSpecificationFlagId;
        product.StarredProduct = dto.StarredProduct;
        product.StarredProductAlias = dto.StarredProductAlias;
        product.MustDeclare = dto.MustDeclare;
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
