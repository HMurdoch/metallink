using Microsoft.AspNetCore.Mvc;
using MetalLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MetalLinkDbContext _dbContext;

    public HealthController(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        // Attempt a trivial query against Customers
        var customers = await _dbContext.Customers.CountAsync();
        var tickets = await _dbContext.Tickets.CountAsync();
        var companies = await _dbContext.Companies.CountAsync();
        var sites = await _dbContext.Sites.CountAsync();
        var products = await _dbContext.Products.CountAsync();

        return Ok(new
        {
            status = "ok",
            customersCount = customers,
            ticketsCount = tickets,
            companiesCount = companies,
            sitesCount = sites,
            productsCount = products
        });
    }
}
