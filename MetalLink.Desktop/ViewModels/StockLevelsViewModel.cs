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

    private bool _isChartExpanded = false;
    public bool IsChartExpanded { get => _isChartExpanded; set => SetProperty(ref _isChartExpanded, value); }

    private bool _isResultsExpanded = false;
    public bool IsResultsExpanded { get => _isResultsExpanded; set => SetProperty(ref _isResultsExpanded, value); }

    private bool _suppressFilterSync;
    private readonly ApiClient _api;

    public ObservableCollection<string> ProductLetterFilters { get; } = new();

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

    private long? _hoveredProductId;
    public long? HoveredProductId
    {
        get => _hoveredProductId;
        set => SetProperty(ref _hoveredProductId, value);
    }

    private Avalonia.Media.IBrush CreateBrush(long productId)
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

        ProductLetterFilters.Add("ALL");
        for (var c = 'A'; c <= 'Z'; c++) ProductLetterFilters.Add(c.ToString());

        ClearProductCommand = new RelayCommand(() =>
        {
            SelectedProduct = null;
            ProductSearchText = string.Empty;
            SelectedProductLetter = "ALL";
        });

        Pagination.PageChanged += (_, __) => ApplyPaging();
        Pagination.PageSize = 20;
    }

    private void ApplyPaging()
    {
        var skip = Pagination.GetSkip();
        var take = Pagination.GetTake();
        var page = _allRows.Skip(skip).Take(take).ToList();

        StockRows = new ObservableCollection<StockLevelLookupDto>(page);
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

            // Load matching products + their stock weight
            var url = $"api/stock-levels/lookup?take=500";
            if (!string.IsNullOrWhiteSpace(term))
                url += $"&term={Uri.EscapeDataString(term)}";
            if (!string.IsNullOrWhiteSpace(letter) && letter != "ALL")
                url += $"&letter={Uri.EscapeDataString(letter)}";

            var results = await _api.GetAsync<StockLevelLookupDto[]>(url, ct) ?? Array.Empty<StockLevelLookupDto>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Auto-expand results and chart on refresh IF we have data or filters
                if (results.Length > 0 || !string.IsNullOrWhiteSpace(term) || (!string.IsNullOrWhiteSpace(letter) && letter != "ALL"))
                {
                    IsChartExpanded = true;
                    IsResultsExpanded = true;
                }

                ProductSuggestions = new ObservableCollection<StockLevelLookupDto>(results);

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
