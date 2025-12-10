using Microsoft.EntityFrameworkCore;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;

namespace MetalLink.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly MetalLinkDbContext _dbContext;

    public CompanyRepository(MetalLinkDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Company>> LookupCompaniesAsync(
        string? term,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim().ToLower();
            query = query.Where(c => c.CompanyName.ToLower().Contains(t));
        }

        return await query
            .OrderBy(c => c.CompanyName)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Site>> LookupSitesForCompanyAsync(
        long companyId,
        string? term,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Sites
            .Include(s => s.Province)
            .Where(s => s.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim().ToLower();
            query = query.Where(s =>
                s.SiteName.ToLower().Contains(t) ||
                (s.SiteCode != null && s.SiteCode.ToLower().Contains(t)));
        }

        return await query
            .OrderBy(s => s.SiteName)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Province>> GetAllProvincesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Provinces
            .OrderBy(p => p.ProvinceName)
            .ToListAsync(cancellationToken);
    }
}
