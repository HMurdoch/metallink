using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Shared.Products;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.Services;

public sealed class ProductsService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public ProductsService(ApiClient apiClient, AuthState authState)
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
    public async Task<(IReadOnlyList<ProductLookupDto> Items, int TotalCount)> LookupProductsAsync(
        string? term,
        int? groupId = null,
        string? letter = null,
        bool includeNonStarred = false,
        int skip = 0,
        int take = 25,
        CancellationToken ct = default)
    {
        var path = $"api/products/lookup?includeNonStarred={includeNonStarred}&skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(term))
            path += $"&term={Uri.EscapeDataString(term)}";
        if (groupId.HasValue && groupId.Value > 0)
            path += $"&groupId={groupId.Value}";
        if (!string.IsNullOrWhiteSpace(letter) && letter != "ALL")
            path += $"&letter={Uri.EscapeDataString(letter)}";

        var result = await _apiClient.GetAsync<PagedResult<ProductLookupDto>>(path, ct);
        return (result?.Items ?? Array.Empty<ProductLookupDto>(), result?.TotalCount ?? 0);
    }

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
    }

    public async Task<IReadOnlyList<ProductGroupDto>> GetProductGroupsAsync(CancellationToken ct = default)
    {
        var result = await _apiClient.GetAsync<ProductGroupDto[]>("api/products/groups", ct);
        return result ?? Array.Empty<ProductGroupDto>();
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

    // ============================================================
    // PRODUCT PRICE LISTS
    // ============================================================

    public async Task<IReadOnlyList<ProductPriceListDto>> GetPriceListsAsync(CancellationToken ct = default)
    {
        var result = await _apiClient.GetAsync<ProductPriceListDto[]>("api/product-price-lists", ct);
        return result ?? Array.Empty<ProductPriceListDto>();
    }

    public Task<ProductPriceListDto?> CreatePriceListAsync(ProductPriceListDto dto, CancellationToken ct = default)
        => _apiClient.PostAsync<ProductPriceListDto, ProductPriceListDto>("api/product-price-lists", dto, ct);

    public async Task UpdatePriceListAsync(int id, ProductPriceListDto dto, CancellationToken ct = default)
    {
        await _apiClient.PutAsJsonAsync($"api/product-price-lists/{id}", dto, ct);
    }

    public Task DeletePriceListAsync(int id, CancellationToken ct = default)
        => _apiClient.DeleteAsync($"api/product-price-lists/{id}", ct);

    public async Task<decimal> GetProductPriceAsync(int productId, int priceListId, CancellationToken ct = default)
    {
        var result = await _apiClient.GetAsync<decimal>($"api/product-price-lists/{priceListId}/products/{productId}/price", ct);
        return result;
    }

    public async Task SetProductPriceAsync(int productId, int priceListId, decimal price, CancellationToken ct = default)
    {
        await _apiClient.PostAsJsonAsync($"api/product-price-lists/{priceListId}/products/{productId}/price", price, ct);
    }
}
