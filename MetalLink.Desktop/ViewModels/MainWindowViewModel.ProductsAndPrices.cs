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
    // Master cache for products (letter filtering)
    private readonly ObservableCollection<ProductLookupDto> _allProducts = new();
    private bool _productLettersLoaded = false;

    // Commands
    public IAsyncRelayCommand SearchProductsCommand { get; private set; } = null!;
    public IAsyncRelayCommand CreateProductCommand { get; private set; } = null!;
    public IAsyncRelayCommand UpdateProductCommand { get; private set; } = null!;
    public IRelayCommand<ProductLookupDto> EditProductCommand { get; private set; } = null!;
    public IAsyncRelayCommand<ProductLookupDto> DeleteProductCommand { get; private set; } = null!;
    public IAsyncRelayCommand ClearProductFormCommand { get; private set; } = null!;
    public IAsyncRelayCommand RefreshProductsCommand { get; private set; } = null!;

    public IAsyncRelayCommand UpdatePriceCommand { get; private set; } = null!;

    // Properties
    public bool IsProductEditMode => EditingProductId.HasValue;
    public bool IsProductCreateMode => !EditingProductId.HasValue;
    public bool IsPriceEditMode => EditingPriceId.HasValue;
    public string ProductSaveButtonText => IsProductEditMode ? "Update" : "Create";

    public bool CanCreateProduct =>
        IsProductCreateMode && !string.IsNullOrWhiteSpace(ProductName) && !string.IsNullOrWhiteSpace(ProductCode);

    public bool CanUpdateProduct =>
        IsProductEditMode && !string.IsNullOrWhiteSpace(ProductName) && !string.IsNullOrWhiteSpace(ProductCode);

    public bool CanUpdatePrice =>
        SelectedProduct != null;

    /// <summary>
    /// Call this ONCE from your constructor in MWVM.Core.cs
    /// </summary>
    private void InitializeProductsAndPricesCommands()
    {
        SearchProductsCommand = new AsyncRelayCommand(ct => SearchProductsAsync(ct));
        CreateProductCommand = new AsyncRelayCommand(ct => CreateProductAsync(ct), () => CanCreateProduct);
        UpdateProductCommand = new AsyncRelayCommand(ct => UpdateProductAsync(ct), () => CanUpdateProduct);
        EditProductCommand = new RelayCommand<ProductLookupDto>(OnEditProduct);
        DeleteProductCommand = new AsyncRelayCommand<ProductLookupDto>(DeleteProductAsync);
        ClearProductFormCommand = new AsyncRelayCommand(ct => ClearProductFormAsync(ct));
        RefreshProductsCommand = new AsyncRelayCommand(ct => RefreshProductsAsync(ct));

        UpdatePriceCommand = new AsyncRelayCommand(ct => UpdatePriceAsync(ct), () => CanUpdatePrice);

        // Load product letters on initialization
        _ = LoadProductsAndLettersAsync();
        _ = LoadProductPriceListsAsync();
    }

    private async Task LoadProductPriceListsAsync(CancellationToken ct = default)
    {
        try
        {
            var lists = await _app.ProductsAndPricesService.GetPriceListsAsync(ct);
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
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            StatusMessage = "Searching products...";

            // Ensure lookup cache is loaded
            if (!_productLettersLoaded)
            {
                await LoadProductsAndLettersAsync();
            }

            // Start from cached master list - only active products
            var query = _allProducts.Where(p => p.IsActive);

            // Apply letter filter
            var letter = (SelectedProductLetter ?? "ALL").Trim();

            if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(letter))
            {
                var ch = char.ToUpperInvariant(letter[0]);
                query = query.Where(p =>
                    !string.IsNullOrWhiteSpace(p.ProductName) &&
                    char.ToUpperInvariant(p.ProductName![0]) == ch);
            }

            // Populate dropdown suggestions with filtered products
            SearchProductSuggestions.Clear();
            foreach (var p in query.OrderBy(p => p.ProductName))
                SearchProductSuggestions.Add(p);

            // Populate results grid
            ProductResults.Clear();
            foreach (var p in query.OrderBy(p => p.ProductName))
                ProductResults.Add(p);

            SelectedProduct = ProductResults.FirstOrDefault();

            StatusMessage = $"[STATUS] Found {ProductResults.Count} product(s).";

            // Load prices for selected product
            if (SelectedProduct != null)
                await LoadPricesForSelectedProductAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Product search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateProductAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (!CanCreateProduct) return;

        IsBusy = true;

        try
        {
            StatusMessage = "[STATUS] Creating product...";

            var product = await _app.ProductsAndPricesService.CreateProductAsync(
                new ProductDto
                {
                    ProductCode = string.IsNullOrWhiteSpace(ProductCode) ? null! : ProductCode.Trim(),
                    ProductName = ProductName.Trim(),
                    Grade = ProductGrade > 0 ? ProductGrade.ToString() : null,
                    MustDeclare = ProductMustDeclare,
                    IsActive = true
                },
                ct);

            if (product == null)
                throw new Exception("CreateProduct returned null.");

            // Clear form
            ProductCode = string.Empty;
            ProductName = string.Empty;
            ProductGrade = 0;

            StatusMessage = $"[STATUS] Created product {product.ProductName}.";

            // Add to cache and select
            var lookup = new ProductLookupDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ProductCode = product.ProductCode,
                Grade = product.Grade,
                IsActive = product.IsActive
            };

            Dispatcher.UIThread.Post(() =>
            {
                AddProductToCachesAndSelect(lookup);
            }, DispatcherPriority.Background);
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

    private void OnEditProduct(ProductLookupDto? product)
    {
        if (product == null) return;

        SelectedProduct = product;
        EditingProductId = product.ProductId;

        ProductCode = product.ProductCode ?? "";
        ProductName = product.ProductName ?? "";
        ProductGrade = string.IsNullOrEmpty(product.Grade) ? 0 : decimal.Parse(product.Grade);
        ProductMustDeclare = product.MustDeclare; // Note: Ensure ProductLookupDto has MustDeclare

        OnPropertyChanged(nameof(IsProductEditMode));
        OnPropertyChanged(nameof(IsProductCreateMode));
        OnPropertyChanged(nameof(CanUpdateProduct));
        OnPropertyChanged(nameof(CanCreateProduct));
        OnPropertyChanged(nameof(ProductSaveButtonText));
        (UpdateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        (CreateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

        StatusMessage = $"[STATUS] Editing product {product.ProductId}.";
    }

    private async Task UpdateProductAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (SelectedProduct == null) return;
        if (!CanUpdateProduct) return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Updating product...";

            var dto = new ProductDto
            {
                ProductId = SelectedProduct.ProductId,
                ProductCode = string.IsNullOrWhiteSpace(ProductCode) ? null! : ProductCode.Trim(),
                ProductName = ProductName.Trim(),
                Grade = ProductGrade > 0 ? ProductGrade.ToString() : null,
                MustDeclare = ProductMustDeclare,
                IsActive = true
            };

            await _app.ProductsAndPricesService.UpdateProductAsync(SelectedProduct.ProductId, dto, ct);

            var idx = ProductResults.IndexOf(SelectedProduct);
            if (idx >= 0)
            {
                var updatedLookup = new ProductLookupDto
                {
                    ProductId = SelectedProduct.ProductId,
                    ProductName = dto.ProductName,
                    ProductCode = dto.ProductCode!,
                    Grade = dto.Grade,
                    IsActive = true
                };

                ProductResults[idx] = updatedLookup;
                SelectedProduct = updatedLookup;
            }

            // Keep cache in sync
            var cache = _allProducts?.FirstOrDefault(x => x.ProductId == dto.ProductId);
            if (cache != null)
            {
                cache.ProductName = dto.ProductName;
                cache.ProductCode = dto.ProductCode!;
                cache.Grade = dto.Grade;
            }

            OnPropertyChanged(nameof(ProductResults));
            OnPropertyChanged(nameof(SelectedProduct));

            StatusMessage = $"[STATUS] Product updated: {dto.ProductName}";
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
            await _app.ProductsAndPricesService.DeleteProductAsync(product.ProductId, ct);

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
                EditingProductId = null;
                ProductCode = "";
                ProductName = "";
                ProductGrade = 0;
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
        ProductCode = string.Empty;
        ProductName = string.Empty;
        ProductGrade = 0;

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

    private async Task LoadProductsAndLettersAsync()
    {
        try
        {
            var items = await _app.ProductsAndPricesService.LookupProductsAsync(string.Empty);

            _allProducts.Clear();
            // Only add active products to cache
            foreach (var p in items.Where(p => p.IsActive).OrderBy(p => p.ProductName))
                _allProducts.Add(p);

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

            SelectedProductLetter ??= "ALL";

            ApplyProductLetterFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load product letters: {ex.Message}";
            _productLettersLoaded = true;
        }
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
            CurrentPrice = await _app.ProductsAndPricesService.GetProductPriceAsync(
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
            await _app.ProductsAndPricesService.SetProductPriceAsync(
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
}
