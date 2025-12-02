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
        var count = await _dbContext.Customers.CountAsync();

        return Ok(new
        {
            status = "ok",
            customersCount = count
        });
    }
}
