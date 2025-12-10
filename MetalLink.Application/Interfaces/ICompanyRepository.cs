using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface ICompanyRepository
{
    Task<IReadOnlyList<Company>> LookupCompaniesAsync(
        string? term,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Site>> LookupSitesForCompanyAsync(
        long companyId,
        string? term,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Province>> GetAllProvincesAsync(
        CancellationToken cancellationToken = default);
}
