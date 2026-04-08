using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Prices;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/product-price-lists")]
public class ProductPriceListsController : ControllerBase
{
    private readonly IProductPriceListRepository _priceListRepo;
    private readonly IProductPriceListProductPriceRepository _priceRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ProductPriceListsController(
        IProductPriceListRepository priceListRepo,
        IProductPriceListProductPriceRepository priceRepo,
        IUnitOfWork unitOfWork)
    {
        _priceListRepo = priceListRepo;
        _priceRepo = priceRepo;
        _unitOfWork = unitOfWork;
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

        return Ok(query.OrderBy(l => l.ProductPriceListName).Select(l => new ProductPriceListDto
        {
            ProductPriceListId = l.ProductPriceListId,
            ProductPriceListName = l.ProductPriceListName,
            ProductPriceListDescription = l.ProductPriceListDescription,
            EntityFlag = l.EntityFlag,
            IsActive = l.IsActive,
            CreatedTime = l.CreatedTime,
            UpdatedTime = l.UpdatedTime
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
}
