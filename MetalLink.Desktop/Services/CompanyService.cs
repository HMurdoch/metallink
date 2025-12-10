using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Shared.Companies;

namespace MetalLink.Desktop.Services;

public sealed class CompanyService
{
    private readonly ApiClient _apiClient;

    public CompanyService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Type-ahead lookup for companies.
    /// </summary>
    public async Task<IReadOnlyList<CompanyLookupDto>> LookupCompaniesAsync(string? query)
    {
        // Controller route: GET api/companies/lookup?term=foo
        var path = "api/companies/lookup";

        if (!string.IsNullOrWhiteSpace(query))
        {
            path += $"?term={Uri.EscapeDataString(query)}";
        }

        var result = await _apiClient.GetAsync<CompanyLookupDto[]>(path);
        return result ?? Array.Empty<CompanyLookupDto>();
    }
}

