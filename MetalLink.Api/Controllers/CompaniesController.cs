// File: MetalLink.Api/Controllers/CompaniesController.cs
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _companyRepository;

    public CompaniesController(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    // GET api/companies/lookup?term=ap
    [HttpGet("lookup")]
    public async Task<ActionResult<IEnumerable<CompanyLookupDto>>> LookupCompanies(
        [FromQuery] string? term,
        CancellationToken cancellationToken)
    {
        var items = await _companyRepository.LookupCompaniesAsync(term, cancellationToken);

        var dtos = items.Select(c => new CompanyLookupDto
        {
            CompanyId   = c.CompanyId,
            CompanyName = c.CompanyName,
            VatNumber   = c.VatNumber,
            Taxable     = c.Taxable
        });

        return Ok(dtos);
    }

    // GET api/companies/{companyId}/sites/lookup?term=jo
    [HttpGet("{companyId:long}/sites/lookup")]
    public async Task<ActionResult<IEnumerable<SiteLookupDto>>> LookupSitesForCompany(
        long companyId,
        [FromQuery] string? term,
        CancellationToken cancellationToken)
    {
        var items = await _companyRepository.LookupSitesForCompanyAsync(
            companyId, term, cancellationToken);

        var dtos = items.Select(s => new SiteLookupDto
        {
            SiteId      = s.SiteId,
            CompanyId   = s.CompanyId,
            SiteName    = s.SiteName,
            SiteCode    = s.SiteCode,
            AddressLine1 = s.AddressLine1,
            AddressLine2 = s.AddressLine2,
            Suburb       = s.Suburb,
            City         = s.City,
            PostalCode   = s.PostalCode,
            ProvinceId   = s.ProvinceId,
            ProvinceName = s.Province?.ProvinceName
        });

        return Ok(dtos);
    }
}
