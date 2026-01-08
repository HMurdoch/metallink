using System;
using System.Collections.Generic;
using System.Threading;
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

    public async Task<IReadOnlyList<CompanyLookupDto>> LookupCompaniesAsync(string term, CancellationToken ct = default)
    {
        var url = "api/companies/lookup";
        if (!string.IsNullOrWhiteSpace(term))
            url += $"?term={Uri.EscapeDataString(term)}";

        var result = await _apiClient.GetAsync<CompanyLookupDto[]>(url, ct);
        return result ?? Array.Empty<CompanyLookupDto>();
    }

    public Task<CompanyLookupDto?> GetCompanyByIdAsync(long companyId, CancellationToken ct = default)
        => _apiClient.GetAsync<CompanyLookupDto>($"api/companies/{companyId}", ct);

    public Task<CompanyLookupDto?> CreateCompanyAsync(CompanyCreateDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<CompanyCreateDto, CompanyLookupDto>("api/companies", dto, ct);

    public Task DeleteCompanyAsync(long companyId, CancellationToken ct = default)
        => _apiClient.DeleteAsync($"api/companies/{companyId}", ct);
}
