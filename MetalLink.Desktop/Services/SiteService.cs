using System.Collections.Generic;
using System.Threading;
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

    // ✅ Only ONE way to load sites for a company: GET /api/sites/lookup
    public Task<List<SiteLookupDto>?> LookupSitesForCompanyAsync(
        long companyId,
        string term = "",
        CancellationToken ct = default)
        => _apiClient.GetAsync<List<SiteLookupDto>>(
            $"api/sites/lookup?companyId={companyId}&term={System.Uri.EscapeDataString(term ?? "")}",
            ct);

    // ✅ Create site: POST /api/sites
    public Task<SiteLookupDto?> CreateSiteAsync(SiteCreateDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<SiteCreateDto, SiteLookupDto>("api/sites", dto, ct);

    public async Task<string?> UploadSiteDocumentAsync(
        int siteId,
        string docType,
        byte[] data,
        string contentType = "image/png",
        CancellationToken ct = default)
    {
        var request = new { ImageData = data, ContentType = contentType };
        var response = await _apiClient.PostAsync<object, UploadDocResponse>(
            $"api/sites/{siteId}/documents/{docType}",
            request,
            ct);
        return response?.DocumentPath;
    }

    public Task<byte[]?> DownloadSiteDocumentAsync(
        int siteId,
        string docType,
        CancellationToken ct = default)
    {
        return _apiClient.GetAsync<byte[]?>(
            $"api/sites/{siteId}/documents/{docType}",
            ct);
    }

    private sealed class UploadDocResponse
    {
        public string? DocumentPath { get; set; }
    }
}
