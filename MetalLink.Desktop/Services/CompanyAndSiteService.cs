using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;

namespace MetalLink.Desktop.Services;

public sealed class CompanyAndSiteService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public CompanyAndSiteService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    // ============================================================
    // COMPANIES
    // ============================================================

    /// <summary>
    /// GET api/companies/lookup?term=foo
    /// </summary>
    public async Task<IReadOnlyList<CompanyLookupDto>> LookupCompaniesAsync(
        string? term,
        CancellationToken ct = default)
    {
        var path = "api/companies/lookup";
        if (!string.IsNullOrWhiteSpace(term))
            path += $"?term={Uri.EscapeDataString(term)}";

        var result = await _apiClient.GetAsync<CompanyLookupDto[]>(path, ct);
        return result ?? Array.Empty<CompanyLookupDto>();
    }

    /// <summary>
    /// GET api/companies/{companyId}
    /// </summary>
    public Task<CompanyDto?> GetCompanyAsync(long companyId, CancellationToken ct = default)
        => _apiClient.GetAsync<CompanyDto>($"api/companies/{companyId}", ct);

    /// <summary>
    /// POST api/companies
    /// </summary>
    public Task<CompanyDto?> CreateCompanyAsync(CompanyDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<CompanyDto, CompanyDto>("api/companies", dto, ct);

    /// <summary>
    /// PUT api/companies/{companyId}
    /// </summary>
    public async Task UpdateCompanyAsync(long companyId, CompanyDto dto, CancellationToken ct = default)
    {
        var response = await _apiClient.PutAsJsonAsync($"api/companies/{companyId}", dto, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
    }

    /// <summary>
    /// UI says "Delete" — soft delete (IsActive=false) using PUT.
    /// </summary>
    public async Task DeleteCompanyAsync(long companyId, CancellationToken ct = default)
    {
        var company = await GetCompanyAsync(companyId, ct)
                      ?? throw new InvalidOperationException($"Company {companyId} not found.");

        company.IsActive = false;
        await UpdateCompanyAsync(companyId, company, ct);
    }

    // ============================================================
    // SITES
    // ============================================================

    /// <summary>
    /// GET api/companies/{companyId}/sites/lookup?term=foo
    /// </summary>
    public async Task<IReadOnlyList<SiteLookupDto>> LookupSitesForCompanyAsync(
        long companyId,
        string? term,
        CancellationToken ct = default)
    {
        var path = $"api/companies/{companyId}/sites/lookup";
        if (!string.IsNullOrWhiteSpace(term))
            path += $"?term={Uri.EscapeDataString(term)}";

        var result = await _apiClient.GetAsync<SiteLookupDto[]>(path, ct);
        return result ?? Array.Empty<SiteLookupDto>();
    }

    /// <summary>
    /// GET api/sites/{siteId}
    /// </summary>
    public Task<SiteDto?> GetSiteAsync(long siteId, CancellationToken ct = default)
        => _apiClient.GetAsync<SiteDto>($"api/sites/{siteId}", ct);

    /// <summary>
    /// POST api/sites
    /// </summary>
    public Task<SiteDto?> CreateSiteAsync(SiteDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<SiteDto, SiteDto>("api/sites", dto, ct);

    /// <summary>
    /// PUT api/sites/{siteId}
    /// </summary>
    public async Task UpdateSiteAsync(long siteId, SiteDto dto, CancellationToken ct = default)
    {
        var response = await _apiClient.PutAsJsonAsync($"api/sites/{siteId}", dto, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
    }

    /// <summary>
    /// UI says "Delete" — soft delete (IsActive=false) using PUT.
    /// </summary>
    public async Task DeleteSiteAsync(long siteId, CancellationToken ct = default)
    {
        var site = await GetSiteAsync(siteId, ct)
                  ?? throw new InvalidOperationException($"Site {siteId} not found.");

        site.IsActive = false;
        await UpdateSiteAsync(siteId, site, ct);
    }
}
