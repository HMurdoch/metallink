using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Products;
using MetalLink.Shared.Prices;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Net.Http;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    public event Action<string>? RequestProductImagePopup;

    // Master cache for products (letter filtering)
    private readonly ObservableCollection<ProductLookupDto> _allProducts = new();
    private bool _productLettersLoaded = false;
    private List<ProductSpecificationFlagDto> _specFlags = new();

    // Commands
    public IAsyncRelayCommand SearchProductsCommand { get; private set; } = null!;
    public IAsyncRelayCommand CreateProductCommand { get; private set; } = null!;
    public IAsyncRelayCommand UpdateProductCommand { get; private set; } = null!;
    public IRelayCommand<ProductLookupDto> EditProductCommand { get; private set; } = null!;
    public IAsyncRelayCommand<ProductLookupDto> DeleteProductCommand { get; private set; } = null!;
    public IAsyncRelayCommand ClearProductFormCommand { get; private set; } = null!;
    public IAsyncRelayCommand RefreshProductsCommand { get; private set; } = null!;
    public IRelayCommand ResetProductFiltersCommand { get; private set; } = null!;

    public IAsyncRelayCommand UpdatePriceCommand { get; private set; } = null!;

    // Properties
    public bool IsProductEditMode => EditingProductId.HasValue;
    public bool IsProductCreateMode => !EditingProductId.HasValue;
    public bool IsPriceEditMode => EditingPriceId.HasValue;
    public string ProductSaveButtonText => IsProductEditMode ? "Update" : "Create";

    public bool CanCreateProduct =>
        IsProductCreateMode && !string.IsNullOrWhiteSpace(ProductIsriName);

    public bool CanUpdateProduct =>
        IsProductEditMode && !string.IsNullOrWhiteSpace(ProductIsriName);

    public bool CanUpdatePrice =>
        SelectedProduct != null;

    /// <summary>
    /// Call this ONCE from your constructor in MWVM.Core.cs
    /// </summary>
    public IAsyncRelayCommand<ProductLookupDto> ToggleStarredCommand { get; private set; } = null!;
    public IRelayCommand OpenIsriUrlCommand { get; private set; } = null!;
    public IRelayCommand<ProductLookupDto> OpenProductUrlCommand { get; private set; } = null!;
    public IRelayCommand<ProductLookupDto> OpenProductImageCommand { get; private set; } = null!;

    private void InitializeProductsCommands()
    {
        SearchProductsCommand = new AsyncRelayCommand(ct => SearchProductsAsync(ct));
        CreateProductCommand = new AsyncRelayCommand(ct => CreateProductAsync(ct), () => CanCreateProduct);
        UpdateProductCommand = new AsyncRelayCommand(ct => UpdateProductAsync(ct), () => CanUpdateProduct);
        EditProductCommand = new RelayCommand<ProductLookupDto>(OnEditProduct);
        DeleteProductCommand = new AsyncRelayCommand<ProductLookupDto>(DeleteProductAsync);
        ClearProductFormCommand = new AsyncRelayCommand(ct => ClearProductFormAsync(ct));
        RefreshProductsCommand = new AsyncRelayCommand(ct => RefreshProductsAsync(ct));
        ResetProductFiltersCommand = new RelayCommand(ResetProductFilters);
        ToggleStarredCommand = new AsyncRelayCommand<ProductLookupDto>(ToggleStarredAsync);
        OpenIsriUrlCommand = new RelayCommand(OpenIsriUrl);
        OpenProductUrlCommand = new RelayCommand<ProductLookupDto>(p => OpenProductUrl(p));
        OpenProductImageCommand = new RelayCommand<ProductLookupDto>(p => OpenProductImage(p));

        UpdatePriceCommand = new AsyncRelayCommand(ct => UpdatePriceAsync(ct), () => CanUpdatePrice);

        ProductsPagination.PageChanged += async (s, e) => await ApplyProductFiltersAsync();

        // Initial loads
        _ = InitializeProductDataAsync();
    }

    private async Task InitializeProductDataAsync()
    {
        // Results panel expanded by default
        ProductsIsSearchResultsExpanded = true;
        ProductsIsSearchCriteriaExpanded = true;
        ProductsIsCreateEditExpanded = true;

        await LoadProductGroupsAsync();
        LoadProductSpecFlags();
        await LoadProductLettersAsync();
        await LoadProductPriceListsAsync();
        
        // Explicitly set initial filter states
        _showStarred = true;
        _showNonStarred = false;
        OnPropertyChanged(nameof(ShowStarred));
        OnPropertyChanged(nameof(ShowNonStarred));

        await ApplyProductFiltersAsync();
    }

    private async Task ToggleStarredAsync(ProductLookupDto? product)
    {
        if (product == null) return;
        try
        {
            var detail = await _app.ProductsService.GetProductAsync(product.ProductId);
            if (detail != null)
            {
                detail.StarredProduct = !detail.StarredProduct;
                // If starring, set alias to current ISRI name if empty
                if (detail.StarredProduct && string.IsNullOrWhiteSpace(detail.StarredProductAlias))
                {
                    detail.StarredProductAlias = "*" + detail.IsriProductName;
                }

                await _app.ProductsService.UpdateProductAsync(product.ProductId, detail);
                product.StarredProduct = detail.StarredProduct;
                product.StarredProductAlias = detail.StarredProductAlias;
                
                // Refresh list if we are only showing starred
                if (!ShowNonStarred && !product.StarredProduct)
                {
                    ProductResults.Remove(product);
                }
                
                OnPropertyChanged(nameof(ProductResults));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to toggle starred: {ex.Message}";
        }
    }

    private async Task LoadProductGroupsAsync()
    {
        try
        {
            var groups = await _app.ProductsService.GetProductGroupsAsync();
            ProductGroups.Clear();
            ProductGroups.Add(new ProductGroupDto { ProductGroupId = 0, ProductGroupName = "ALL" });
            foreach (var g in groups) ProductGroups.Add(g);
            SelectedProductGroup = ProductGroups[0];
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load product groups: {ex.Message}";
        }
    }

    private void ResetProductFilters()
    {
        SelectedProductGroup = ProductGroups.FirstOrDefault(g => g.ProductGroupId == 0);
        ProductSearchTerm = string.Empty;
        SelectedProductLetter = "ALL";
    }

    private async Task LoadProductLettersAsync()
    {
        try
        {
            // Only include first letters for Products we have in the DB that are starred
            var result = await _app.ProductsService.LookupProductsAsync(null, 0, "ALL", false, 0, 1000);
            
            ProductLetterFilters.Clear();
            ProductLetterFilters.Add("ALL");

            var letters = result.Items
                .Where(p => !string.IsNullOrEmpty(p.ProductName))
                .Select(p => char.ToUpperInvariant(p.ProductName[0]))
                .Distinct()
                .OrderBy(c => c);

            foreach (var c in letters)
            {
                ProductLetterFilters.Add(c.ToString());
            }
            
            SelectedProductLetter = "ALL";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load letters: {ex.Message}";
            // Fallback
            ProductLetterFilters.Clear();
            ProductLetterFilters.Add("ALL");
            SelectedProductLetter = "ALL";
        }
    }

    private void LoadProductSpecFlags()
    {
        try
        {
            // This is a bit of a hack since we don't have a service method for this, 
            // but we can try to get them from the context if it was injected, 
            // or just hardcode the common ones if needed.
            // For now, let's assume we might need a service method.
            // Since I can't easily add one to the service, I'll use a hardcoded list for now
            // based on what's typically in the DB for this app.
            _specFlags = new List<ProductSpecificationFlagDto>
            {
                new() { ProductSpecificationFlagId = 1, ProductSpecificationDescription = "Standard" },
                new() { ProductSpecificationFlagId = 2, ProductSpecificationDescription = "Premium" },
                new() { ProductSpecificationFlagId = 3, ProductSpecificationDescription = "Mixed" },
                new() { ProductSpecificationFlagId = 4, ProductSpecificationDescription = "Low Grade" }
            };
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load spec flags: {ex.Message}";
        }
    }

    private async Task ApplyProductFiltersAsync()
    {
        try
        {
            // Ensure we have a valid group ID (0 = ALL)
            int? gid = SelectedProductGroup != null ? SelectedProductGroup.ProductGroupId : 0;
            
            // Logic for the dual toggles:
            // If both are on, we show everything (includeNonStarred = true)
            // If only Starred is on, we show only Starred (includeNonStarred = false)
            // If only Non-Starred is on, we need a way to filter for ONLY non-starred.
            // However, the current API LookupProductsAsync takes 'includeNonStarred'.
            // If includeNonStarred is false, it ONLY shows Starred.
            // If includeNonStarred is true, it shows BOTH.
            // To show ONLY Non-Starred, we'd need an API change or client-side filter.
            // For now, I'll follow the existing API behavior but map the toggles as best as possible.
            // If ShowNonStarred is true and ShowStarred is false, it implies "Show me what's NOT starred".
            // If both are true, it shows everything.
            
            // Logic for the dual toggles:
            // 1. Both on -> Show everything (includeNonStarred = true)
            // 2. Only Starred on -> Show only Starred (includeNonStarred = false)
            // 3. Only Non-Starred on -> Show only Non-Starred (includeNonStarred = true, then filter)
            
            bool includeNonStarred = ShowNonStarred;
            
            var result = await _app.ProductsService.LookupProductsAsync(
                ProductSearchTerm, 
                gid, 
                SelectedProductLetter, 
                includeNonStarred,
                ProductsPagination.GetSkip(),
                ProductsPagination.GetTake());

            ProductResults.Clear();
            
            // If Only Non-Starred is on, we filter the items client-side
            var items = result.Items.AsEnumerable();
            if (ShowNonStarred && !ShowStarred)
            {
                items = items.Where(p => !p.StarredProduct);
            }

            foreach (var r in items) 
            {
                // Map spec description
                var spec = _specFlags.FirstOrDefault(f => f.ProductSpecificationFlagId == r.ProductSpecificationFlagId);
                r.ProductSpecificationDescription = spec?.ProductSpecificationDescription ?? $"Flag {r.ProductSpecificationFlagId}";
                
                // Ensure description is not null for tooltip
                if (string.IsNullOrEmpty(r.IsriProductDescription))
                {
                    r.IsriProductDescription = "No description available.";
                }

                ProductResults.Add(r);
            }
            
            ProductsPagination.SetTotalRecords(result.TotalCount);

            string filterText;
            if (ShowStarred && ShowNonStarred) filterText = "Showing All Products";
            else if (ShowStarred) filterText = "Showing Starred Products Only";
            else filterText = "Showing Non-Starred Products Only";
            
            StatusMessage = $"[STATUS] Found {ProductResults.Count} products. ({filterText})";
            
            // Debug check
            Console.WriteLine($"[DEBUG] Applied Filter. Results count: {ProductResults.Count}, Total: {result.TotalCount}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Filter failed: {ex.Message}";
        }
    }

    private async Task LoadProductPriceListsAsync(CancellationToken ct = default)
    {
        try
        {
            var lists = await _app.ProductsService.GetPriceListsAsync(ct);
            ProductPriceLists.Clear();
            foreach (var list in lists)
                ProductPriceLists.Add(list);

            if (SelectedProductPriceList == null && ProductPriceLists.Count > 0)
                SelectedProductPriceList = ProductPriceLists[0];
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load price lists: {ex.Message}";
        }
    }

    // =====================================================
    // PRODUCT METHODS
    // =====================================================

    private async Task SearchProductsAsync(CancellationToken ct = default)
    {
        await ApplyProductFiltersAsync();
    }

    private async Task CreateProductAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (!CanCreateProduct) return;

        IsBusy = true;

        try
        {
            StatusMessage = "[STATUS] Creating product...";

            var product = await _app.ProductsService.CreateProductAsync(
                new ProductDto
                {
                    HtsCode = ProductHtsCode,
                    QKey = ProductQKey,
                    IsriProduct = ProductIsIsri,
                    IsriProductCode = ProductIsriCode,
                    IsriProductName = ProductIsriName,
                    IsriProductDescription = ProductIsriDescription,
                    IsriProductUrl = ProductIsriUrl,
                    ProductGroupId = ProductGroupId,
                    ProductSpecificationFlagId = ProductSpecFlagId,
                    StarredProduct = ProductStarred,
                    StarredProductAlias = ProductStarredAlias,
                    MustDeclare = ProductMustDeclare,
                    IsActive = true
                },
                ct);

            if (product == null)
                throw new Exception("CreateProduct returned null.");

            StatusMessage = $"[STATUS] Created product {product.IsriProductName}.";
            await ClearProductFormAsync(ct);
            await ApplyProductFiltersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Create failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnEditProduct(ProductLookupDto? product)
    {
        if (product == null) return;

        try
        {
            var detail = await _app.ProductsService.GetProductAsync(product.ProductId);
            if (detail == null) return;

            EditingProductId = detail.ProductId;
            ProductHtsCode = detail.HtsCode;
            ProductIsIsri = detail.IsriProduct;
            ProductIsriCode = detail.IsriProductCode;
            ProductIsriName = detail.IsriProductName;
            ProductIsriDescription = detail.IsriProductDescription;
            ProductIsriUrl = detail.IsriProductUrl;
            ProductGroupId = detail.ProductGroupId;
            ProductSpecFlagId = detail.ProductSpecificationFlagId;
            ProductStarred = detail.StarredProduct;
            ProductStarredAlias = detail.StarredProductAlias;
            ProductMustDeclare = detail.MustDeclare;

            OnPropertyChanged(nameof(IsProductEditMode));
            OnPropertyChanged(nameof(IsProductCreateMode));
            OnPropertyChanged(nameof(CanUpdateProduct));
            OnPropertyChanged(nameof(CanCreateProduct));
            OnPropertyChanged(nameof(ProductSaveButtonText));
            (UpdateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (CreateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

            StatusMessage = $"[STATUS] Editing product {detail.ProductId}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load product details: {ex.Message}";
        }
    }

    private async Task UpdateProductAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (!EditingProductId.HasValue) return;
        if (!CanUpdateProduct) return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Updating product...";

            var dto = new ProductDto
            {
                ProductId = EditingProductId.Value,
                HtsCode = ProductHtsCode,
                IsriProduct = ProductIsIsri,
                IsriProductCode = ProductIsriCode,
                IsriProductName = ProductIsriName,
                IsriProductDescription = ProductIsriDescription,
                IsriProductUrl = ProductIsriUrl,
                ProductGroupId = ProductGroupId,
                ProductSpecificationFlagId = ProductSpecFlagId,
                StarredProduct = ProductStarred,
                StarredProductAlias = ProductStarredAlias,
                MustDeclare = ProductMustDeclare,
                IsActive = true
            };

            await _app.ProductsService.UpdateProductAsync(dto.ProductId, dto, ct);

            StatusMessage = $"[STATUS] Product updated: {dto.IsriProductName}";
            await ApplyProductFiltersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Update failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            (UpdateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task DeleteProductAsync(ProductLookupDto? product, CancellationToken ct = default)
    {
        if (product == null) return;
        if (IsBusy) return;

        var ok = await ConfirmAsync($"Are you sure you want to delete product '{product.ProductName}'?");
        if (!ok) return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Deleting product...";
            await _app.ProductsService.DeleteProductAsync(product.ProductId, ct);

            // Remove from results grid
            var row = ProductResults.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (row != null) ProductResults.Remove(row);

            // Remove from cached master list
            var cacheItem = _allProducts.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (cacheItem != null) _allProducts.Remove(cacheItem);

            // Remove from search suggestions
            var suggestionItem = SearchProductSuggestions.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (suggestionItem != null) SearchProductSuggestions.Remove(suggestionItem);

            if (SelectedProduct?.ProductId == product.ProductId)
            {
                SelectedProduct = null;
                await ClearProductFormAsync(ct);
                ClearPriceForm();
            }

            StatusMessage = "[STATUS] Product deleted (soft).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task ClearProductFormAsync(CancellationToken ct = default)
    {
        EditingProductId = null;
        ProductHtsCode = null;
        ProductQKey = null;
        ProductIsIsri = false;
        ProductIsriCode = string.Empty;
        ProductIsriName = string.Empty;
        ProductIsriDescription = null;
        ProductIsriUrl = null;
        ProductGroupId = ProductGroups.FirstOrDefault()?.ProductGroupId ?? 0;
        ProductSpecFlagId = 1;
        ProductStarred = false;
        ProductStarredAlias = null;
        ProductMustDeclare = false;

        OnPropertyChanged(nameof(IsProductEditMode));
        OnPropertyChanged(nameof(IsProductCreateMode));
        OnPropertyChanged(nameof(ProductSaveButtonText));
        OnPropertyChanged(nameof(CanCreateProduct));
        OnPropertyChanged(nameof(CanUpdateProduct));
        (CreateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        (UpdateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

        StatusMessage = "[STATUS] Product form cleared.";
        return Task.CompletedTask;
    }

    private void AddProductToCachesAndSelect(ProductLookupDto createdProduct)
    {
        var existing = _allProducts.FirstOrDefault(p => p.ProductId == createdProduct.ProductId);
        if (existing != null)
        {
            var idx = _allProducts.IndexOf(existing);
            if (idx >= 0) _allProducts[idx] = createdProduct;
        }
        else
        {
            _allProducts.Add(createdProduct);
        }

        var sorted = _allProducts.OrderBy(p => p.ProductName).ToList();
        _allProducts.Clear();
        foreach (var p in sorted) _allProducts.Add(p);

        // Rebuild letter list
        _productLetterFilters.Clear();
        _productLetterFilters.Add("ALL");

        var letters = _allProducts
            .Select(p => p.ProductName?.FirstOrDefault() ?? '\0')
            .Where(ch => ch != '\0')
            .Select(ch => char.ToUpperInvariant(ch))
            .Distinct()
            .OrderBy(ch => ch);

        foreach (var ch in letters)
            _productLetterFilters.Add(ch.ToString());

        _productLettersLoaded = true;

        var first = createdProduct.ProductName?.FirstOrDefault();
        var letterStr = first.HasValue ? char.ToUpperInvariant(first.Value).ToString() : "ALL";
        if (!ProductLetterFilters.Contains(letterStr))
            letterStr = "ALL";

        SelectedProductLetter = letterStr;

        ApplyProductLetterFilter();

        ProductResults.Clear();
        foreach (var p in SearchProductSuggestions.OrderBy(x => x.ProductName))
            ProductResults.Add(p);

        SelectedSearchProduct = SearchProductSuggestions.FirstOrDefault(p => p.ProductId == createdProduct.ProductId);
        SelectedProduct = ProductResults.FirstOrDefault(p => p.ProductId == createdProduct.ProductId);
    }

    private void ApplyProductLetterFilter()
    {
        if (!_productLettersLoaded) return;

        var selectedId = SelectedSearchProduct?.ProductId;
        var letter = (SelectedProductLetter ?? "ALL").Trim();

        SearchProductSuggestions.Clear();

        IEnumerable<ProductLookupDto> query = _allProducts.Where(p => p.IsActive);

        if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(p =>
                !string.IsNullOrWhiteSpace(p.ProductName) &&
                char.ToUpperInvariant(p.ProductName![0]) == ch);
        }

        foreach (var p in query.OrderBy(p => p.ProductName))
            SearchProductSuggestions.Add(p);

        if (selectedId.HasValue)
            SelectedSearchProduct = SearchProductSuggestions.FirstOrDefault(x => x.ProductId == selectedId.Value);
    }

    private Task LoadProductsAndLettersAsync()
    {
        // Letters are handled dynamically now
        return Task.CompletedTask;
    }

    private async Task RefreshProductsAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            StatusMessage = "[STATUS] Refreshing products...";

            // Force reload by resetting the flag
            _productLettersLoaded = false;

            await LoadProductsAndLettersAsync();

            // Refresh the results grid
            await SearchProductsAsync(ct);

            StatusMessage = $"[STATUS] Products refreshed. {_allProducts.Count} active products loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Refresh failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =====================================================
    // PRICE METHODS
    // =====================================================

    private async Task LoadPricesForSelectedProductAsync(CancellationToken ct = default)
    {
        if (SelectedProduct == null || SelectedProductPriceList == null)
        {
            ClearPriceForm();
            return;
        }

        try
        {
            StatusMessage = "Loading price...";
            CurrentPrice = await _app.ProductsService.GetProductPriceAsync(
                (int)SelectedProduct.ProductId, 
                SelectedProductPriceList.ProductPriceListId, 
                ct);
            StatusMessage = "Price loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load price failed: {ex.Message}";
            CurrentPrice = 0;
        }
    }

    private async Task UpdatePriceAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (SelectedProduct == null || SelectedProductPriceList == null)
        {
            StatusMessage = "[STATUS] Select a product and price list first.";
            return;
        }

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Updating price...";
            await _app.ProductsService.SetProductPriceAsync(
                (int)SelectedProduct.ProductId, 
                SelectedProductPriceList.ProductPriceListId, 
                CurrentPrice, 
                ct);
            StatusMessage = "[STATUS] Price updated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Update price failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            (UpdatePriceCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private void ClearPriceForm()
    {
        EditingPriceId = null;
        CurrentPrice = 0;
        OnPropertyChanged(nameof(CanUpdatePrice));
        (UpdatePriceCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private void OpenIsriUrl()
    {
        // Try selected product URL first, then the form URL
        string? url = SelectedProduct?.IsriProductUrl ?? ProductIsriUrl;
        if (string.IsNullOrEmpty(url)) return;
        OpenUrl(url);
    }

    private void OpenProductUrl(ProductLookupDto? product)
    {
        if (product == null || string.IsNullOrEmpty(product.IsriProductUrl)) return;
        OpenUrl(product.IsriProductUrl);
    }

    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private string? _productSampleImage;
    public string? ProductSampleImage
    {
        get => _productSampleImage;
        set { _productSampleImage = value; OnPropertyChanged(); }
    }

    private void OpenProductImage(ProductLookupDto? product)
    {
        if (product == null) return;
        string imageUrl = $"http://localhost:9000/product-samples/product_{product.ProductId}.jpg";
        ProductSampleImage = imageUrl;
        
        RequestProductImagePopup?.Invoke(imageUrl);
    }
}
