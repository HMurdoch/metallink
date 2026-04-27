using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Configuration;
using MetalLink.Shared.Stock;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthState _authState;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(AuthState authState)
    {
        _authState = authState;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl)
        };
        
        // Configure JSON serialization to handle camelCase from API
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
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

    public async Task<TResponse?> GetAsync<TResponse>(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();

        Console.WriteLine($"GET: {_httpClient.BaseAddress}{relativeUrl}");

        var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");
        }

        // Special handling for byte arrays (binary data like images)
        if (typeof(TResponse) == typeof(byte[]))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return (TResponse)(object)bytes;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken: cancellationToken);
    }


    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();

        var response = await _httpClient.PostAsJsonAsync(
            relativeUrl,
            requestBody,
            _jsonOptions,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("========== API POST ERROR ==========");
            Console.WriteLine($"URL: {relativeUrl}");
            Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine("REQUEST BODY:");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(responseBody);
            Console.WriteLine("====================================");

            throw new HttpRequestException(
                $"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(
            _jsonOptions,
            cancellationToken: cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(
        string uri,
        T body,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        Console.WriteLine($"PUT: {_httpClient.BaseAddress}{uri}");
        
        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = JsonContent.Create(body)
        };

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string uri,
        T body,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        Console.WriteLine($"POST: {_httpClient.BaseAddress}{uri}");
        
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(body)
        };

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        ApplyAuthHeader();

        Console.WriteLine($"DELETE: {_httpClient.BaseAddress}{relativeUrl}");

        var response = await _httpClient.DeleteAsync(relativeUrl, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");
    }

    public async Task<List<PriceListStockLevelDto>> GetPriceListStockLevelsAsync(
        char entityFlag,
        int[]? selectedPriceListIds = null,
        int? productGroupId = null,
        string? searchTerm = null,
        string? letter = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        ApplyAuthHeader();

        var queryParams = new
        {
            entityType = entityFlag == 'C' ? "Customer" : "Buyer",
            selectedPriceListIds,
            productGroupId,
            searchTerm,
            letter,
            skip,
            take
        };

        var url = $"api/stock-levels/price-lists{ToQueryString(queryParams)}";
        Console.WriteLine($"GET: {_httpClient.BaseAddress}{url}");

        var response = await _httpClient.GetAsync(url, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");

        return JsonSerializer.Deserialize<List<PriceListStockLevelDto>>(raw, _jsonOptions) ?? new List<PriceListStockLevelDto>();
    }

    public async Task<List<PriceListStockMovementDto>> GetPriceListStockMovementsAsync(
        char entityFlag,
        int[]? selectedPriceListIds = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? productId = null,
        string? movementType = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        ApplyAuthHeader();

        var queryParams = new
        {
            entityType = entityFlag == 'C' ? "Customer" : "Buyer",
            selectedPriceListIds,
            fromDate = fromDate?.ToString("yyyy-MM-dd"),
            toDate = toDate?.ToString("yyyy-MM-dd"),
            productId,
            movementType,
            skip,
            take
        };

        var url = $"api/stock-movements/price-lists{ToQueryString(queryParams)}";
        Console.WriteLine($"GET: {_httpClient.BaseAddress}{url}");

        var response = await _httpClient.GetAsync(url, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");

        return JsonSerializer.Deserialize<List<PriceListStockMovementDto>>(raw, _jsonOptions) ?? new List<PriceListStockMovementDto>();
    }

    public async Task<List<ProductPriceListDto>> GetPriceListsAsync(char entityFlag, CancellationToken ct = default)
    {
        ApplyAuthHeader();

        var entityType = entityFlag == 'C' ? "Customer" : "Buyer";
        var url = $"api/product-price-lists?entityType={Uri.EscapeDataString(entityType)}";
        Console.WriteLine($"GET: {_httpClient.BaseAddress}{url}");

        var response = await _httpClient.GetAsync(url, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");

        return JsonSerializer.Deserialize<List<ProductPriceListDto>>(raw, _jsonOptions) ?? new List<ProductPriceListDto>();
    }

    public string ToQueryString(object? values)
    {
        if (values == null) return string.Empty;

        var props = new List<string>();
        foreach (var p in values.GetType().GetProperties())
        {
            var value = p.GetValue(values, null);
            if (value != null)
            {
                if (value is IEnumerable<int> intArray && intArray.Any())
                {
                    foreach (var item in intArray)
                    {
                        props.Add($"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(item.ToString())}");
                    }
                }
                else
                {
                    props.Add($"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(value.ToString()!)}");
                }
            }
        }

        var qs = string.Join("&", props);
        return string.IsNullOrEmpty(qs) ? string.Empty : "?" + qs;
    }
}
