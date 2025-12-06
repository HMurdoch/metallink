using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Configuration;
using MetalLink.Shared.Customers;

namespace MetalLink.Desktop.Services;

public sealed class DocumentService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;
    private readonly HttpClient _httpClient;

    public DocumentService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl)
        };
    }

    private void ApplyAuthHeader()
    {
        if (_authState.IsAuthenticated)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authState.Token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<IReadOnlyList<CustomerDocumentDto>> GetDocumentsAsync(
        long customerId,
        CancellationToken cancellationToken = default)
    {
        // Use ApiClient for simple GET JSON
        var docs = await _apiClient.GetAsync<List<CustomerDocumentDto>>(
            $"api/customers/{customerId}/documents",
            cancellationToken
        );

        return docs ?? new List<CustomerDocumentDto>();
    }

    public async Task<CustomerDocumentDto?> UploadDocumentAsync(
        long customerId,
        string documentType,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        ApplyAuthHeader();

        await using var fs = File.OpenRead(filePath);
        var fileName = Path.GetFileName(filePath);

        var content = new MultipartFormDataContent();

        // Document type field
        var typeContent = new StringContent(documentType);
        content.Add(typeContent, "documentType");

        // File field
        var fileContent = new StreamContent(fs);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", fileName);

        var url = $"api/customers/{customerId}/documents";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomerDocumentDto>(
            cancellationToken: cancellationToken);

        return result;
    }
}
