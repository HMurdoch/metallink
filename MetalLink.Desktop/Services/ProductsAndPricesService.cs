using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Products;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.Services;

public sealed class ProductsAndPricesService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public ProductsAndPricesService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    // ============================================================
    // PRODUCTS
    // ============================================================

    /// <summary>
    /// GET api/products/lookup?term=foo
    /// </summary>
    public async Task<IReadOnlyList<ProductLookupDto>> LookupProductsAsync(
        string? term,
        CancellationToken ct = default)
    {
        var path = "api/products/lookup";
        if (!string.IsNullOrWhiteSpace(term))
            path += $"?term={Uri.EscapeDataString(term)}";

        var result = await _apiClient.GetAsync<ProductLookupDto[]>(path, ct);
        return result ?? Array.Empty<ProductLookupDto>();
    }

    /// <summary>
    /// GET api/products/{productId}
    /// </summary>
    public Task<ProductDto?> GetProductAsync(long productId, CancellationToken ct = default)
        => _apiClient.GetAsync<ProductDto>($"api/products/{productId}", ct);

    /// <summary>
    /// POST api/products
    /// </summary>
    public Task<ProductDto?> CreateProductAsync(ProductDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<ProductDto, ProductDto>("api/products", dto, ct);

    /// <summary>
    /// PUT api/products/{productId}
    /// </summary>
    public async Task UpdateProductAsync(long productId, ProductDto dto, CancellationToken ct = default)
    {
        var response = await _apiClient.PutAsJsonAsync($"api/products/{productId}", dto, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
    }

    /// <summary>
    /// DELETE api/products/{productId} (soft delete)
    /// </summary>
    public Task DeleteProductAsync(long productId, CancellationToken ct = default)
        => _apiClient.DeleteAsync($"api/products/{productId}", ct);

    // ============================================================
    // PRICES
    // ============================================================

    /// <summary>
    /// GET api/prices/product/{productId} - Get single price for a product
    /// </summary>
    public Task<PriceDto?> GetPriceForProductAsync(
        long productId,
        CancellationToken ct = default)
        => _apiClient.GetAsync<PriceDto>($"api/prices/product/{productId}", ct);

    /// <summary>
    /// GET api/prices/{priceId}
    /// </summary>
    public Task<PriceDto?> GetPriceAsync(long priceId, CancellationToken ct = default)
        => _apiClient.GetAsync<PriceDto>($"api/prices/{priceId}", ct);

    /// <summary>
    /// POST api/prices
    /// </summary>
    public Task<PriceDto?> CreatePriceAsync(PriceDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<PriceDto, PriceDto>("api/prices", dto, ct);

    /// <summary>
    /// PUT api/prices/{priceId}
    /// </summary>
    public async Task UpdatePriceAsync(long priceId, PriceDto dto, CancellationToken ct = default)
    {
        var response = await _apiClient.PutAsJsonAsync($"api/prices/{priceId}", dto, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
    }

    /// <summary>
    /// DELETE api/prices/{priceId} (soft delete)
    /// </summary>
    public Task DeletePriceAsync(long priceId, CancellationToken ct = default)
        => _apiClient.DeleteAsync($"api/prices/{priceId}", ct);
}
