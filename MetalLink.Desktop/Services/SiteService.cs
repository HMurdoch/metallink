using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Shared.Sites;

namespace MetalLink.Desktop.Services;

public sealed class SiteService
{
    private readonly ApiClient _apiClient;

    public SiteService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Type-ahead lookup for sites for a given company.
    /// </summary>
    public async Task<IReadOnlyList<SiteLookupDto>> LookupSitesForCompanyAsync(
        long companyId,
        string? query)
    {
        // Controller route: GET api/companies/{companyId}/sites/lookup?term=foo
        var path = $"api/companies/{companyId}/sites/lookup";

        if (!string.IsNullOrWhiteSpace(query))
        {
            path += $"?term={Uri.EscapeDataString(query)}";
        }

        var result = await _apiClient.GetAsync<SiteLookupDto[]>(path);
        return result ?? Array.Empty<SiteLookupDto>();
    }
}
