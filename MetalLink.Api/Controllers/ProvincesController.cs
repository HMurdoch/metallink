using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Locations;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvincesController : ControllerBase
{
    private readonly ICompanyRepository _companyRepository;

    public ProvincesController(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    // GET api/provinces
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProvinceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _companyRepository.GetAllProvincesAsync(cancellationToken);

        var dtos = items.Select(p => new ProvinceDto
        {
            ProvinceId   = p.ProvinceId,
            ProvinceName = p.ProvinceName,
            IsActive     = p.IsActive,
            CreatedTime  = p.CreatedTime,
            UpdatedTime  = p.UpdatedTime
        });

        return Ok(dtos);
    }
}
