using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Prices;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/product-price-lists")]
public class ProductPriceListsController : ControllerBase
{
    private readonly IProductPriceListRepository _priceListRepo;
    private readonly IProductPriceListProductPriceRepository _priceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MetalLinkDbContext _db;

    public ProductPriceListsController(
        IProductPriceListRepository priceListRepo,
        IProductPriceListProductPriceRepository priceRepo,
        IUnitOfWork unitOfWork,
        MetalLinkDbContext db)
    {
        _priceListRepo = priceListRepo;
        _priceRepo = priceRepo;
        _unitOfWork = unitOfWork;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductPriceListDto>>> GetAll(
        [FromQuery] string? term,
        [FromQuery] string? entityType)
    {
        var lists = await _priceListRepo.GetAllActiveAsync();
        
        var query = lists.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            char flag = entityType.Equals("Buyer", StringComparison.OrdinalIgnoreCase) ? 'B' : 'C';
            query = query.Where(l => l.EntityFlag == flag);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim().ToLower();
            query = query.Where(l => 
                (l.ProductPriceListName != null && l.ProductPriceListName.ToLower().Contains(t)) ||
                (l.ProductPriceListDescription != null && l.ProductPriceListDescription.ToLower().Contains(t)));
        }

        var orderedIds = query.OrderBy(l => l.ProductPriceListName)
            .Select(l => l.ProductPriceListId)
            .ToList();

        var productCounts = await _db.ProductPriceListProductPrices
            .IgnoreQueryFilters()
            .Where(p => p.IsActive && orderedIds.Contains(p.ProductPriceListId))
            .GroupBy(p => p.ProductPriceListId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return Ok(query.OrderBy(l => l.ProductPriceListName).Select(l => new ProductPriceListDto
        {
            ProductPriceListId = l.ProductPriceListId,
            ProductPriceListName = l.ProductPriceListName,
            ProductPriceListDescription = l.ProductPriceListDescription,
            EntityFlag = l.EntityFlag.ToString(),
            IsActive = l.IsActive,
            CreatedTime = l.CreatedTime,
            UpdatedTime = l.UpdatedTime,
            ProductCount = productCounts.GetValueOrDefault(l.ProductPriceListId, 0)
        }));
    }

    [HttpGet("{priceListId}/prices")]
    public async Task<ActionResult<IEnumerable<ProductPriceDto>>> GetPricesByList(int priceListId)
    {
        var prices = await _priceRepo.GetByPriceListIdAsync(priceListId);
        return Ok(prices.Select(p => new ProductPriceDto
        {
            ProductId = p.ProductId,
            Price = p.Price
        }));
    }

    [HttpGet("{priceListId}/products/{productId}/price")]
    public async Task<ActionResult<decimal>> GetProductPrice(int priceListId, int productId)
    {
        var price = await _priceRepo.GetByProductAndListAsync(productId, priceListId);
        return Ok(price?.Price ?? 0);
    }

    [HttpPost("{priceListId}/products/{productId}/price")]
    public async Task<IActionResult> SetProductPrice(int priceListId, int productId, [FromBody] decimal price)
    {
        var existing = await _priceRepo.GetByProductAndListAsync(productId, priceListId);
        if (existing != null)
        {
            existing.Price = price;
            existing.UpdatedTime = DateTimeOffset.UtcNow;
            _priceRepo.Update(existing);
        }
        else
        {
            await _priceRepo.AddAsync(new ProductPriceListProductPrice
            {
                ProductPriceListId = priceListId,
                ProductId = productId,
                Price = price,
                CreatedByOperatorId = 1, // Default for now
                IsActive = true,
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<ProductPriceListDto>> Create([FromBody] ProductPriceListDto dto)
    {
        var entity = new ProductPriceList
        {
            ProductPriceListName = dto.ProductPriceListName,
            ProductPriceListDescription = dto.ProductPriceListDescription,
            EntityFlag = dto.EntityFlag?.Length > 0 ? dto.EntityFlag[0] : 'C',
            IsActive = true,
            CreatedByOperatorId = 1,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow
        };
        await _priceListRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        dto.ProductPriceListId = entity.ProductPriceListId;
        dto.CreatedTime = entity.CreatedTime;
        dto.UpdatedTime = entity.UpdatedTime;
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductPriceListDto dto)
    {
        var entity = await _priceListRepo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        entity.ProductPriceListName = dto.ProductPriceListName;
        entity.ProductPriceListDescription = dto.ProductPriceListDescription;
        entity.EntityFlag = dto.EntityFlag?.Length > 0 ? dto.EntityFlag[0] : 'C';
        entity.IsActive = dto.IsActive;
        entity.UpdatedTime = DateTimeOffset.UtcNow;
        _priceListRepo.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _priceListRepo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        entity.IsActive = false;
        entity.UpdatedTime = DateTimeOffset.UtcNow;
        _priceListRepo.Update(entity);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{priceListId}/seed")]
    public async Task<IActionResult> SeedPrices(int priceListId, [FromBody] SeedPriceListRequestDto request)
    {
        var priceList = await _priceListRepo.GetByIdAsync(priceListId);
        if (priceList == null) return NotFound();

        Console.WriteLine($"[SEED] priceListId={priceListId} UseLastKnownPrice={request.UseLastKnownPrice} CloneFrom={request.CloneFromPriceListId}");

        Dictionary<int, decimal> prices;
        if (request.UseLastKnownPrice)
        {
            // Use the last recorded unit price from the movement tables.
            // Receiving movements (BUY from customers) → Customer price lists (entity_flag='C').
            // Sending  movements (SELL to buyers)      → Buyer    price lists (entity_flag='B').
            if (priceList.EntityFlag == 'C')
            {
                var movements = await _db.StockMovementReceiving
                    .IgnoreQueryFilters()
                    .Where(m => m.IsActive)
                    .Select(m => new { m.ProductId, m.UnitPricePerKg, m.MovementDate })
                    .ToListAsync();
                prices = movements
                    .GroupBy(m => m.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(m => m.MovementDate).First().UnitPricePerKg);
            }
            else
            {
                var movements = await _db.StockMovementSending
                    .IgnoreQueryFilters()
                    .Where(m => m.IsActive)
                    .Select(m => new { m.ProductId, m.UnitPricePerKg, m.MovementDate })
                    .ToListAsync();
                prices = movements
                    .GroupBy(m => m.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(m => m.MovementDate).First().UnitPricePerKg);
            }
            Console.WriteLine($"[SEED] Found {prices.Count} products with movement history.");
        }
        else if (request.CloneFromPriceListId.HasValue)
        {
            var sourcePrices = await _priceRepo.GetByPriceListIdAsync(request.CloneFromPriceListId.Value);
            prices = sourcePrices.ToDictionary(p => p.ProductId, p => p.Price);
            Console.WriteLine($"[SEED] Cloning {prices.Count} products from price list {request.CloneFromPriceListId}.");
        }
        else
        {
            return BadRequest("Specify UseLastKnownPrice=true or provide CloneFromPriceListId.");
        }

        foreach (var (productId, price) in prices)
        {
            if (price <= 0) continue; // Skip zero/negative prices
            var existing = await _priceRepo.GetByProductAndListAsync(productId, priceListId);
            if (existing != null)
            {
                existing.Price = price;
                existing.UpdatedTime = DateTimeOffset.UtcNow;
                _priceRepo.Update(existing);
            }
            else
            {
                await _priceRepo.AddAsync(new ProductPriceListProductPrice
                {
                    ProductPriceListId = priceListId,
                    ProductId = productId,
                    Price = price,
                    CreatedByOperatorId = 1,
                    IsActive = true,
                    CreatedTime = DateTimeOffset.UtcNow,
                    UpdatedTime = DateTimeOffset.UtcNow
                });
            }
        }
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }
}
