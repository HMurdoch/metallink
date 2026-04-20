using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Products;
using MetalLink.Shared.Stock;

namespace MetalLink.Desktop.ViewModels;

public sealed class StockLevelsViewModel : ViewModelBase
    {
    private bool _isFilterExpanded = true;
    public bool IsFilterExpanded { get => _isFilterExpanded; set => SetProperty(ref _isFilterExpanded, value); }

    private bool _isChartExpanded = true;
    public bool IsChartExpanded { get => _isChartExpanded; set => SetProperty(ref _isChartExpanded, value); }

    private bool _isResultsExpanded = true;
    public bool IsResultsExpanded { get => _isResultsExpanded; set => SetProperty(ref _isResultsExpanded, value); }

    private bool _suppressFilterSync;
    private readonly ApiClient _api;

    public ObservableCollection<string> ProductLetterFilters { get; } = new();
    public ObservableCollection<ProductGroupDto> ProductGroups { get; } = new();

    private ProductGroupDto? _selectedProductGroup;
    public ProductGroupDto? SelectedProductGroup
    {
        get => _selectedProductGroup;
        set
        {
            if (SetProperty(ref _selectedProductGroup, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    private string _productSearchText = string.Empty;
    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (!SetProperty(ref _productSearchText, value))
                return;

            if (_suppressFilterSync)
            {
                _ = RefreshAsync();
                return;
            }

            // Rule: changing text clears first-letter filter
            if (!string.IsNullOrWhiteSpace(_productSearchText) && SelectedProductLetter != "ALL")
            {
                _suppressFilterSync = true;
                SelectedProductLetter = "ALL";
                _suppressFilterSync = false;
            }

            _ = RefreshAsync();
        }
    }

    private string _selectedProductLetter = "ALL";
    public string SelectedProductLetter
    {
        get => _selectedProductLetter;
        set
        {
            if (!SetProperty(ref _selectedProductLetter, value))
                return;

            if (_suppressFilterSync)
            {
                _ = RefreshAsync();
                return;
            }

            // Rule: changing first-letter clears text filter (including ALL)
            if (!string.IsNullOrWhiteSpace(_productSearchText))
            {
                _suppressFilterSync = true;
                _productSearchText = string.Empty;
                OnPropertyChanged(nameof(ProductSearchText));
                _suppressFilterSync = false;
            }

            _ = RefreshAsync();
        }
    }

    private ObservableCollection<StockLevelLookupDto> _productSuggestions = new();
    public ObservableCollection<StockLevelLookupDto> ProductSuggestions
    {
        get => _productSuggestions;
        set => SetProperty(ref _productSuggestions, value);
    }

    private StockLevelLookupDto? _selectedProduct;
    public StockLevelLookupDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
                _ = RefreshAsync();
        }
    }

    public PaginationViewModel Pagination { get; } = new();

    public string PaginationStatusText
    {
        get
        {
            if (Pagination.TotalRecords <= 0)
                return "Showing 0 of 0";

            var start = Pagination.GetSkip() + 1;
            var end = Math.Min(Pagination.GetSkip() + Pagination.GetTake(), Pagination.TotalRecords);
            return $"Showing {start}-{end} of {Pagination.TotalRecords}";
        }
    }

    private ObservableCollection<StockLevelLookupDto> _allRows = new();

    private ObservableCollection<StockLevelLookupDto> _stockRows = new();
    public ObservableCollection<StockLevelLookupDto> StockRows
    {
        get => _stockRows;
        set
        {
            if (SetProperty(ref _stockRows, value))
                OnPropertyChanged(nameof(ChartItems));
        }
    }

    public IReadOnlyList<MetalLink.Desktop.Views.Controls.BarChart3DControl.BarItem> ChartItems
        => StockRows
            // IMPORTANT: keep the same order as the DataGrid (including user sorts)
            .Select(r => new MetalLink.Desktop.Views.Controls.BarChart3DControl.BarItem(
                r.ProductId,
                r.ProductName,
                (double)r.WeightKg,
                CreateBrush(r.ProductId)))
            .ToList();

    private int? _hoveredProductId;
    public int? HoveredProductId
    {
        get => _hoveredProductId;
        set
        {
            if (SetProperty(ref _hoveredProductId, value))
            {
                UpdateRowsHoverState();
            }
        }
    }

    private void UpdateRowsHoverState()
    {
        // This is a bit of a hack to force the DataGrid to re-evaluate styles or to manually set a property.
        // Better: Use a property on the DTO or a wrapper. Let's add a list of hovered IDs for style binding if needed,
        // but for now we'll rely on the view code-behind to find the row and add a class.
    }

    private Avalonia.Media.IBrush CreateBrush(int productId)
    {
        // simple stable palette
        var hue = (int)(Math.Abs(productId) % 360);
        var c = Avalonia.Media.Color.FromRgb((byte)(80 + (hue % 175)), (byte)(60 + ((hue * 3) % 150)), (byte)(120 + ((hue * 7) % 120)));
        return new Avalonia.Media.SolidColorBrush(c);
    }

    public ICommand ClearProductCommand { get; }

    private CancellationTokenSource? _cts;

    public StockLevelsViewModel(ApiClient api)
    {
        _api = api;

        _ = InitializeAsync();

        ClearProductCommand = new RelayCommand(() =>
        {
            SelectedProduct = null;
            SelectedProductGroup = ProductGroups.FirstOrDefault(g => g.ProductGroupId == 0);
            ProductSearchText = string.Empty;
            SelectedProductLetter = "ALL";
        });

        Pagination.PageChanged += (_, __) => ApplyPaging();
        Pagination.PageSize = 20;
    }

    private async Task InitializeAsync()
    {
        await LoadProductGroupsAsync();
        await LoadProductLettersAsync();
        
        if (SelectedProductGroup == null && ProductGroups.Count > 0)
        {
            SelectedProductGroup = ProductGroups[0];
        }
        
        await RefreshAsync();
    }

    private async Task LoadProductGroupsAsync()
    {
        try
        {
            var groups = await _api.GetAsync<List<ProductGroupDto>>("api/products/groups") ?? new List<ProductGroupDto>();
            ProductGroups.Clear();
            var allGroup = new ProductGroupDto { ProductGroupId = 0, ProductGroupName = "All" };
            ProductGroups.Add(allGroup);
            foreach (var g in groups) ProductGroups.Add(g);
            
            _selectedProductGroup = allGroup;
            OnPropertyChanged(nameof(SelectedProductGroup));
        }
        catch { /* handle error */ }
    }

    private async Task LoadProductLettersAsync()
    {
        try
        {
            // Requirement: Only include first letters for Products we have in the DB that are starred
            // We use the stock-levels lookup here as it's optimized for what we need
            var result = await _api.GetAsync<List<StockLevelLookupDto>>("api/stock-levels/lookup?take=1000&includeNonStarred=false") ?? new List<StockLevelLookupDto>();
            
            ProductLetterFilters.Clear();
            ProductLetterFilters.Add("ALL");

            var letters = result
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
        catch
        {
            // Fallback
            ProductLetterFilters.Clear();
            ProductLetterFilters.Add("ALL");
            for (var c = 'A'; c <= 'Z'; c++) ProductLetterFilters.Add(c.ToString());
            SelectedProductLetter = "ALL";
        }
    }

    private void ApplyPaging()
    {
        var skip = Pagination.GetSkip();
        var take = Pagination.GetTake();
        var page = _allRows.Skip(skip).Take(take).ToList();

        StockRows = new ObservableCollection<StockLevelLookupDto>(page);
        Console.WriteLine($"[DEBUG] StockLevelsViewModel: ApplyPaging updated StockRows with {StockRows.Count} items. skip={skip}, take={take}");
        OnPropertyChanged(nameof(ChartItems));
        OnPropertyChanged(nameof(PaginationStatusText));
    }

    public void SortAll(string column, bool asc)
    {
        IOrderedEnumerable<StockLevelLookupDto> ordered = column switch
        {
            "Code" => asc ? _allRows.OrderBy(r => r.ProductCode) : _allRows.OrderByDescending(r => r.ProductCode),
            "Product" => asc ? _allRows.OrderBy(r => r.ProductName) : _allRows.OrderByDescending(r => r.ProductName),
            _ => asc ? _allRows.OrderBy(r => r.WeightKg) : _allRows.OrderByDescending(r => r.WeightKg)
        };

        _allRows = new ObservableCollection<StockLevelLookupDto>(ordered.ToList());
        Pagination.SetTotalRecords(_allRows.Count);
        OnPropertyChanged(nameof(PaginationStatusText));
        ApplyPaging();
    }

    public async Task RefreshAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            var term = string.IsNullOrWhiteSpace(ProductSearchText) ? null : ProductSearchText.Trim();
            var letter = string.IsNullOrWhiteSpace(SelectedProductLetter) ? null : SelectedProductLetter.Trim();
            var groupId = SelectedProductGroup?.ProductGroupId ?? 0;

            // Load matching products + their stock weight
            // Rule: only starred products (showNonStarred = false)
            var url = $"api/stock-levels/lookup?take=500&includeNonStarred=false";
            if (!string.IsNullOrWhiteSpace(term))
                url += $"&term={Uri.EscapeDataString(term)}";
            if (groupId > 0)
                url += $"&groupId={groupId}";
            if (!string.IsNullOrWhiteSpace(letter) && letter != "ALL")
                url += $"&letter={Uri.EscapeDataString(letter)}";

            var results = await _api.GetAsync<StockLevelLookupDto[]>(url, ct) ?? Array.Empty<StockLevelLookupDto>();
            Console.WriteLine($"[DEBUG] StockLevelsViewModel: RefreshAsync returned {results.Length} products from {url}.");
            foreach(var r in results.Take(5)) {
                Console.WriteLine($"  - Product: {r.ProductName}, Code: {r.ProductCode}, Weight: {r.WeightKg}kg");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine($"[DEBUG] StockLevelsViewModel: Dispatching UI update. Results count: {results.Length}");
                // Ensure results and chart are expanded on load/refresh
                IsChartExpanded = true;
                IsResultsExpanded = true;

                ProductSuggestions = new ObservableCollection<StockLevelLookupDto>(results);
                Console.WriteLine($"[DEBUG] StockLevelsViewModel: Updated ProductSuggestions with {ProductSuggestions.Count} items.");

                // If a specific product is selected, show only that; otherwise show all.
                var rows = SelectedProduct is null
                    ? results
                    : results.Where(r => r.ProductId == SelectedProduct.ProductId).ToArray();

                _allRows = new ObservableCollection<StockLevelLookupDto>(rows);
                Pagination.SetTotalRecords(_allRows.Count);
                OnPropertyChanged(nameof(PaginationStatusText));
                ApplyPaging();
            });
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockLevels] Refresh failed: {ex.Message}");
        }
    }

    // Local minimal command implementation
    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
