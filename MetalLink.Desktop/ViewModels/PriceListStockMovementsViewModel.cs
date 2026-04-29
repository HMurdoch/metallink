using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Stock;
using MetalLink.Shared.Products;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Desktop.ViewModels;

public partial class PriceListStockMovementsViewModel : ViewModelBase
{
    public partial class PriceListSelectionItem : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private bool isSelected;
    }
}

public partial class PriceListStockMovementsViewModel : ViewModelBase
{

    [ObservableProperty]
    private ObservableCollection<string> _entityTypes = new() { "Customer", "Buyer" };

    [ObservableProperty]
    private string _selectedEntityType = "Customer";

    [ObservableProperty]
    private ObservableCollection<PriceListSelectionItem> _availablePriceLists = new();

    [ObservableProperty]
    private ObservableCollection<string> _movementTypes = new() { "Receiving", "Sending", "Adjustment" };

    [ObservableProperty]
    private string? _selectedMovementType;

    [ObservableProperty]
    private DateTimeOffset? _fromDate;

    [ObservableProperty]
    private DateTimeOffset? _toDate;

    [ObservableProperty]
    private ObservableCollection<PriceListStockMovementDto> _stockMovements = new();

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private int _totalItems;

    // New properties for UI
    [ObservableProperty]
    private bool _isProductFilterExpanded = true;

    [ObservableProperty]
    private bool _isPriceListExpanded = true;

    [ObservableProperty]
    private bool _isDateFilterExpanded = true;

    [ObservableProperty]
    private bool _isResultsExpanded = true;

    [ObservableProperty]
    private ObservableCollection<ProductGroupDto> _productGroups = new();

    [ObservableProperty]
    private ObservableCollection<string> _productLetterFilters = new();

    [ObservableProperty]
    private ProductGroupDto? _selectedProductGroup;

    [ObservableProperty]
    private ObservableCollection<ProductLookupDto> _products = new();

    [ObservableProperty]
    private ProductLookupDto? _selectedProduct;

    [ObservableProperty]
    private string _productSearchText = string.Empty;

    [ObservableProperty]
    private string _selectedProductLetter = "ALL";

    private readonly ApiClient _apiClient;

    public PriceListStockMovementsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var groups = await _apiClient.GetAsync<List<ProductGroupDto>>("api/products/groups");
            ProductGroups.Clear();
            ProductGroups.Add(new ProductGroupDto { ProductGroupId = 0, ProductGroupName = "All Groups" });
            foreach (var group in groups)
                ProductGroups.Add(group);

            SelectedProductGroup = ProductGroups.First();

            ProductLetterFilters.Clear();
            ProductLetterFilters.Add("ALL");
            for (char c = 'A'; c <= 'Z'; c++)
                ProductLetterFilters.Add(c.ToString());

            await LoadProductsAsync();
            LoadPriceLists();
            LoadData();
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    private async Task LoadProductsAsync(CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string> { "includeNonStarred=false", "skip=0", "take=100" };
            if (SelectedProductGroup?.ProductGroupId > 0)
                queryParams.Add($"groupId={SelectedProductGroup.ProductGroupId}");
            if (!string.IsNullOrWhiteSpace(ProductSearchText))
                queryParams.Add($"term={Uri.EscapeDataString(ProductSearchText)}");
            if (!string.IsNullOrWhiteSpace(SelectedProductLetter) && SelectedProductLetter != "ALL")
                queryParams.Add($"letter={Uri.EscapeDataString(SelectedProductLetter)}");

            var path = "api/products/lookup?" + string.Join("&", queryParams);
            var result = await _apiClient.GetAsync<ProductsService.PagedResult<ProductLookupDto>>(path, ct);

            Products.Clear();
            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                    Products.Add(item);
            }
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    public string CurrentPageInfo => $"Page {CurrentPage} of {(TotalItems + PageSize - 1) / PageSize}";

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < (TotalItems + PageSize - 1) / PageSize;

    private async void LoadPriceLists()
    {
        try
        {
            var priceLists = await _apiClient.GetPriceListsAsync(SelectedEntityType == "Customer" ? 'C' : 'B');
            AvailablePriceLists = new ObservableCollection<PriceListSelectionItem>(
                priceLists.Select(pl => new PriceListSelectionItem
                {
                    Id = pl.ProductPriceListId,
                    Name = pl.ProductPriceListName,
                    IsSelected = true
                }));
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    private async void LoadData()
    {
        try
        {
            var selectedIds = AvailablePriceLists.Where(pl => pl.IsSelected).Select(pl => pl.Id).ToArray();
            var movements = await _apiClient.GetPriceListStockMovementsAsync(
                SelectedEntityType == "Customer" ? 'C' : 'B',
                selectedIds,
                FromDate?.DateTime,
                ToDate?.DateTime,
                SelectedProduct?.ProductId,
                SelectedMovementType,
                (CurrentPage - 1) * PageSize,
                PageSize);

            StockMovements = new ObservableCollection<PriceListStockMovementDto>(movements);
            TotalItems = movements.Count; // This should come from API with total count
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    [RelayCommand]
    private void SelectAllPriceLists()
    {
        foreach (var pl in AvailablePriceLists)
        {
            pl.IsSelected = true;
        }
        LoadData();
    }

    [RelayCommand]
    private void ClearAllPriceLists()
    {
        foreach (var pl in AvailablePriceLists)
        {
            pl.IsSelected = false;
        }
        LoadData();
    }

    [RelayCommand]
    private void ClearProductCommand()
    {
        SelectedProductGroup = ProductGroups.First();
        ProductSearchText = string.Empty;
        SelectedProductLetter = "ALL";
        SelectedProduct = null;
        LoadProductsAsync();
        LoadData();
    }

    partial void OnSelectedProductGroupChanged(ProductGroupDto? value)
    {
        _ = LoadProductsAsync();
        LoadData();
    }

    partial void OnProductSearchTextChanged(string value)
    {
        _ = LoadProductsAsync();
        LoadData();
    }

    partial void OnSelectedProductLetterChanged(string value)
    {
        _ = LoadProductsAsync();
        LoadData();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPage--;
            LoadData();
        }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPage++;
            LoadData();
        }
    }

    partial void OnSelectedEntityTypeChanged(string value)
    {
        LoadPriceLists();
        LoadData();
    }

    public async Task RefreshAsync()
    {
        LoadPriceLists();
        LoadData();
    }
}