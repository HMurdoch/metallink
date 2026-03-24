using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Application.Services;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Sites;
using MetalLink.Api.Extensions;
using MetalLink.Application.Interfaces;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/sites")]
public sealed class SitesController : ControllerBase
{
    private readonly MetalLinkDbContext _db;
    private readonly IFileStorage _fileStorage;

    public SitesController(MetalLinkDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    // GET /api/sites/lookup?companyId=8&term=
    [HttpGet("lookup")]
    public async Task<ActionResult<List<SiteLookupDto>>> Lookup(
        [FromQuery] int companyId,
        [FromQuery] string? term = null,
        CancellationToken ct = default)
    {
        term ??= "";

        var query = _db.Sites.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(s => EF.Functions.ILike(s.SiteName!, $"%{t}%"));
        }

        var results = await query
            .OrderBy(s => s.SiteName)
            .Select(s => new SiteLookupDto
            {
                SiteId = s.SiteId,
                CompanyId = s.CompanyId,
                SiteName = s.SiteName,
                ProvinceId = s.ProvinceId,
                CountryId = s.CountryId,
                IsActive = s.IsActive,

                SiteCode = s.SiteCode,
                AddressLine1 = s.AddressLine1,
                AddressLine2 = s.AddressLine2,
                Suburb = s.Suburb,
                City = s.City,
                PostalCode = s.PostalCode,

                CipcDocumentPath = s.DocumentPath != null ? s.DocumentPath.CipcDocumentPath : null,
                TradingLicensePath = s.DocumentPath != null ? s.DocumentPath.TradingLicensePath : null,
                VatRegistrationCertificatePath = s.DocumentPath != null ? s.DocumentPath.VatRegistrationCertificatePath : null,
                TaxClearanceCertificatePath = s.DocumentPath != null ? s.DocumentPath.TaxClearanceCertificatePath : null,
                BbbeeComplianceCertificatePath = s.DocumentPath != null ? s.DocumentPath.BbbeeComplianceCertificatePath : null
            })
            .ToListAsync(ct);

        return Ok(results);
    }

    // POST /api/sites
    [HttpPost]
    public async Task<ActionResult<SiteLookupDto>> Create([FromBody] SiteCreateDto dto, CancellationToken ct)
    {
        if (dto == null)
            return BadRequest("Body required.");

        var name = (dto.SiteName ?? string.Empty).Trim();
        if (dto.CompanyId <= 0)
            return BadRequest("CompanyId is required.");
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("SiteName is required.");

        var company = await _db.Companies.Include(c => c.Sites).FirstOrDefaultAsync(c => c.CompanyId == dto.CompanyId, ct);
        if (company == null)
            return BadRequest($"Company {dto.CompanyId} not found.");

        // Generate site code if not provided
        var siteCode = dto.SiteCode;
        if (string.IsNullOrWhiteSpace(siteCode) || siteCode == "SITE-0")
        {
            siteCode = SiteCodeGeneratorService.GenerateNextSiteCode(company.Sites);
        }

        var operatorId = (int)User.GetOperatorId();

        var entity = new Site
        {
            CompanyId = dto.CompanyId,
            SiteName = name,
            IsActive = dto.IsActive,
            CreatedByOperatorId = operatorId,

            SiteCode = siteCode,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            Suburb = dto.Suburb,
            City = dto.City,
            PostalCode = dto.PostalCode,
            ProvinceId = dto.ProvinceId,
            CountryId = dto.CountryId
        };

        // Handle initial document paths if provided (though usually uploaded later)
        if (!string.IsNullOrWhiteSpace(dto.CipcDocumentPath) || 
            !string.IsNullOrWhiteSpace(dto.TradingLicensePath) || 
            !string.IsNullOrWhiteSpace(dto.VatRegistrationCertificatePath) ||
            !string.IsNullOrWhiteSpace(dto.TaxClearanceCertificatePath) ||
            !string.IsNullOrWhiteSpace(dto.BbbeeComplianceCertificatePath))
        {
            entity.DocumentPath = new DocumentPath
            {
                CipcDocumentPath = dto.CipcDocumentPath,
                TradingLicensePath = dto.TradingLicensePath,
                VatRegistrationCertificatePath = dto.VatRegistrationCertificatePath,
                TaxClearanceCertificatePath = dto.TaxClearanceCertificatePath,
                BbbeeComplianceCertificatePath = dto.BbbeeComplianceCertificatePath,
                CreatedByOperatorId = operatorId
            };
        }

        _db.Sites.Add(entity);
        await _db.SaveChangesAsync(ct);

        var result = new SiteLookupDto
        {
            SiteId = entity.SiteId,
            CompanyId = entity.CompanyId,
            SiteName = entity.SiteName,
            SiteCode = entity.SiteCode,
            AddressLine1 = entity.AddressLine1,
            AddressLine2 = entity.AddressLine2,
            Suburb = entity.Suburb,
            City = entity.City,
            PostalCode = entity.PostalCode,
            ProvinceId = entity.ProvinceId,
            CountryId = entity.CountryId,
            IsActive = entity.IsActive,
            CipcDocumentPath = entity.DocumentPath?.CipcDocumentPath,
            TradingLicensePath = entity.DocumentPath?.TradingLicensePath,
            VatRegistrationCertificatePath = entity.DocumentPath?.VatRegistrationCertificatePath,
            TaxClearanceCertificatePath = entity.DocumentPath?.TaxClearanceCertificatePath,
            BbbeeComplianceCertificatePath = entity.DocumentPath?.BbbeeComplianceCertificatePath
        };

        return Created($"api/sites/{entity.SiteId}", result);
    }

    // PUT /api/sites/{siteId}
    [HttpPut("{siteId:int}")]
    public async Task<IActionResult> Update(int siteId, [FromBody] SiteLookupDto dto, CancellationToken ct)
    {
        var site = await _db.Sites
            .Include(s => s.DocumentPath)
            .FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site == null) return NotFound();

        site.SiteName = dto.SiteName ?? site.SiteName;
        site.SiteCode = dto.SiteCode;
        site.AddressLine1 = dto.AddressLine1;
        site.AddressLine2 = dto.AddressLine2;
        site.Suburb = dto.Suburb;
        site.City = dto.City;
        site.PostalCode = dto.PostalCode;
        site.ProvinceId = dto.ProvinceId ?? site.ProvinceId;
        site.CountryId = dto.CountryId ?? site.CountryId;
        site.IsActive = dto.IsActive;

        // Update document paths if provided
        if (site.DocumentPath == null && (!string.IsNullOrWhiteSpace(dto.CipcDocumentPath) || 
                                          !string.IsNullOrWhiteSpace(dto.TradingLicensePath) || 
                                          !string.IsNullOrWhiteSpace(dto.VatRegistrationCertificatePath) ||
                                          !string.IsNullOrWhiteSpace(dto.TaxClearanceCertificatePath) ||
                                          !string.IsNullOrWhiteSpace(dto.BbbeeComplianceCertificatePath)))
        {
            site.DocumentPath = new DocumentPath { CreatedByOperatorId = (int)User.GetOperatorId() };
        }

        if (site.DocumentPath != null)
        {
            site.DocumentPath.CipcDocumentPath = dto.CipcDocumentPath;
            site.DocumentPath.TradingLicensePath = dto.TradingLicensePath;
            site.DocumentPath.VatRegistrationCertificatePath = dto.VatRegistrationCertificatePath;
            site.DocumentPath.TaxClearanceCertificatePath = dto.TaxClearanceCertificatePath;
            site.DocumentPath.BbbeeComplianceCertificatePath = dto.BbbeeComplianceCertificatePath;
            site.DocumentPath.UpdatedTime = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // -----------------------------
    // UPLOAD SITE DOCUMENT
    // -----------------------------
    [HttpPost("{siteId:int}/documents/{docType}")]
    public async Task<IActionResult> UploadDocument(
        int siteId,
        string docType,
        [FromBody] SiteUploadDocumentRequest request,
        CancellationToken ct)
    {
        if (request.ImageData == null || request.ImageData.Length == 0)
            return BadRequest("Document data is required");

        var validTypes = new[] { "cipc", "trading", "vat", "tax", "bbee" };
        if (!validTypes.Contains(docType.ToLower()))
            return BadRequest($"Invalid document type. Valid types: {string.Join(", ", validTypes)}");

        var extension = request.ContentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "application/pdf" => "pdf",
            _ => "jpg"
        };
        
        var key = $"sites/{siteId}/{docType}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.{extension}";

        await _fileStorage.UploadAsync(
            request.ImageData,
            request.ContentType ?? "image/jpeg",
            key,
            ct);

        var site = await _db.Sites.Include(s => s.DocumentPath).FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site != null)
        {
            if (site.DocumentPath == null)
            {
                site.DocumentPath = new DocumentPath
                {
                    CreatedByOperatorId = (int)User.GetOperatorId()
                };
                _db.DocumentPaths.Add(site.DocumentPath);
            }

            switch (docType.ToLower())
            {
                case "cipc": site.DocumentPath.CipcDocumentPath = key; break;
                case "trading": site.DocumentPath.TradingLicensePath = key; break;
                case "vat": site.DocumentPath.VatRegistrationCertificatePath = key; break;
                case "tax": site.DocumentPath.TaxClearanceCertificatePath = key; break;
                case "bbee": site.DocumentPath.BbbeeComplianceCertificatePath = key; break;
            }
            
            site.UpdatedTime = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { DocumentPath = key });
    }

    // -----------------------------
    // DOWNLOAD SITE DOCUMENT
    // -----------------------------
    [HttpGet("{siteId:int}/documents/{docType}")]
    public async Task<IActionResult> DownloadDocument(
        int siteId,
        string docType,
        CancellationToken ct)
    {
        var site = await _db.Sites.Include(s => s.DocumentPath).FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site?.DocumentPath == null) return NotFound();

        string? path = docType.ToLower() switch
        {
            "cipc" => site.DocumentPath.CipcDocumentPath,
            "trading" => site.DocumentPath.TradingLicensePath,
            "vat" => site.DocumentPath.VatRegistrationCertificatePath,
            "tax" => site.DocumentPath.TaxClearanceCertificatePath,
            "bbee" => site.DocumentPath.BbbeeComplianceCertificatePath,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(path)) return NotFound();

        try
        {
            var url = _fileStorage.GetFileUrl(path, TimeSpan.FromMinutes(5));
            using var httpClient = new HttpClient();
            var data = await httpClient.GetByteArrayAsync(url, ct);
            
            var contentType = path.EndsWith(".pdf") ? "application/pdf" : "image/png";
            return File(data, contentType);
        }
        catch { return NotFound(); }
    }

    // DELETE /api/sites/{siteId} (soft delete)
    [HttpDelete("{siteId:int}")]
    public async Task<IActionResult> Delete(int siteId, CancellationToken ct)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId, ct);
        if (site == null) return NotFound();
        
        if (!site.IsActive)
        {
            return BadRequest("Site is already inactive.");
        }

        // Validation: Count total active sites for this company (including current one)
        var totalActiveSites = await _db.Sites
            .Where(s => s.CompanyId == site.CompanyId && s.IsActive)
            .CountAsync(ct);

        if (totalActiveSites <= 1)
        {
            return BadRequest("Cannot delete the last active site. A company must have at least one active site.");
        }

        site.IsActive = false;
        site.UpdatedTime = DateTimeOffset.UtcNow;
        
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed class SiteUploadDocumentRequest
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string? ContentType { get; set; }
}
