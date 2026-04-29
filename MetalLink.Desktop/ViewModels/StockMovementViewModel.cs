using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Stock;

namespace MetalLink.Desktop.ViewModels;

public sealed class StockMovementViewModel : ViewModelBase
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
    public ObservableCollection<MetalLink.Shared.Products.ProductGroupDto> ProductGroups { get; } = new();

    private MetalLink.Shared.Products.ProductGroupDto? _selectedProductGroup;
    public MetalLink.Shared.Products.ProductGroupDto? SelectedProductGroup
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

            // Rule: choosing a letter clears text filter
            if (!string.IsNullOrWhiteSpace(_selectedProductLetter) && _selectedProductLetter != "ALL" && !string.IsNullOrWhiteSpace(ProductSearchText))
            {
                _suppressFilterSync = true;
                ProductSearchText = string.Empty;
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
            if (!SetProperty(ref _selectedProduct, value))
                return;

            _ = RefreshAsync();
        }
    }

    public PaginationViewModel Pagination { get; } = new();

    public string PaginationStatusText
    {
        get
        {
            if (Pagination.TotalRecords == 0)
                return "No products";

            var start = Pagination.GetSkip() + 1;
            var end = Math.Min(Pagination.GetSkip() + Pagination.GetTake(), Pagination.TotalRecords);
            return $"Showing {start}-{end} of {Pagination.TotalRecords}";
        }
    }

    // Date range
    private bool _allHistory;
    public bool AllHistory
    {
        get => _allHistory;
        set
        {
            if (!SetProperty(ref _allHistory, value)) return;
            OnPropertyChanged(nameof(IsFromPickerEnabled));
            OnPropertyChanged(nameof(IsToPickerEnabled));
            OnPropertyChanged(nameof(IsRangeVisible));
            _ = RefreshSeriesAsync();
        }
    }

    private bool _fromDay0;
    public bool FromDay0
    {
        get => _fromDay0;
        set
        {
            if (!SetProperty(ref _fromDay0, value)) return;
            OnPropertyChanged(nameof(IsFromPickerEnabled));
            if (FromDay0 && ToNow) AllHistory = true;
            _ = RefreshSeriesAsync();
        }
    }

    private bool _toNow;
    public bool ToNow
    {
        get => _toNow;
        set
        {
            if (!SetProperty(ref _toNow, value)) return;
            OnPropertyChanged(nameof(IsToPickerEnabled));
            if (FromDay0 && ToNow) AllHistory = true;
            _ = RefreshSeriesAsync();
        }
    }

    public bool IsFromPickerEnabled => !AllHistory && !FromDay0;
    public bool IsToPickerEnabled => !AllHistory && !ToNow;
    public bool IsRangeVisible => !AllHistory;

    private DateTimeOffset? _fromDate = DateTimeOffset.Now.AddDays(-30);
    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set
        {
            if (!SetProperty(ref _fromDate, value)) return;
            if (IsFromPickerEnabled) _ = RefreshAsync();
        }
    }

    private TimeSpan _fromTime = new(0, 0, 0);
    public TimeSpan FromTime
    {
        get => _fromTime;
        set
        {
            if (!SetProperty(ref _fromTime, value)) return;
            if (IsFromPickerEnabled) _ = RefreshAsync();
        }
    }

    private DateTimeOffset? _toDate = DateTimeOffset.Now;
    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set
        {
            if (!SetProperty(ref _toDate, value)) return;
            if (IsToPickerEnabled) _ = RefreshAsync();
        }
    }

    private TimeSpan _toTime = new(23, 55, 0);
    public TimeSpan ToTime
    {
        get => _toTime;
        set
        {
            if (!SetProperty(ref _toTime, value)) return;
            if (IsToPickerEnabled) _ = RefreshAsync();
        }
    }

    private DateTimeOffset? BuildFromDateTimeOffset()
    {
        if (FromDate is null) return null;
        var localDate = FromDate.Value.ToLocalTime().Date;
        var dt = localDate + FromTime;
        return new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
    }

    private DateTimeOffset? BuildToDateTimeOffset()
    {
        if (ToDate is null) return null;
        var localDate = ToDate.Value.ToLocalTime().Date;
        var dt = localDate + ToTime;
        return new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
    }

    public ObservableCollection<TimeSpan> TimeOptions5Min { get; } = BuildTimeOptions5Min();

    private static ObservableCollection<TimeSpan> BuildTimeOptions5Min()
    {
        var list = new ObservableCollection<TimeSpan>();
        for (var h = 0; h < 24; h++)
        for (var m = 0; m < 60; m += 5)
            list.Add(new TimeSpan(h, m, 0));
        return list;
    }

    public string FormatTime(TimeSpan t) => $"{t.Hours:D2}:{t.Minutes:D2}";

    public sealed class TransactionViewModel : ViewModelBase
    {
        public DateTimeOffset Timestamp { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal OnHandAfter { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public string ChangePrefix => Quantity >= 0 ? "+" : "-";
        public Avalonia.Media.IBrush ChangeBrush => Quantity >= 0 ? Avalonia.Media.Brushes.LimeGreen : Avalonia.Media.Brushes.Tomato;
        public int ProductId { get; set; }
    }

    private ObservableCollection<TransactionViewModel> _transactions = new();
    public ObservableCollection<TransactionViewModel> Transactions
    {
        get => _transactions;
        set => SetProperty(ref _transactions, value);
    }

    private StockLevelLookupDto? _selectedProductRow;
    public StockLevelLookupDto? SelectedProductRow
    {
        get => _selectedProductRow;
        set
        {
            // If the same row is clicked again, we want to DESELECT it.
            // Note: DataGrid might pass null or the same instance. 
            // In Avalonia, if you use a binding, you might need extra logic to handle 'un-selecting'
            // but for now let's assume the property setter is called.
            if (_selectedProductRow == value)
            {
                _selectedProductRow = null;
                OnPropertyChanged();
                _ = RefreshSeriesAsync();
            }
            else if (SetProperty(ref _selectedProductRow, value))
            {
                _ = RefreshSeriesAsync();
            }
        }
    }

    // Rows (paged products)
    private ObservableCollection<StockLevelLookupDto> _allRows = new();

    private ObservableCollection<StockLevelLookupDto> _rows = new();
    public ObservableCollection<StockLevelLookupDto> Rows
    {
        get => _rows;
        set => SetProperty(ref _rows, value);
    }

    // Chart series
    private ISeries[] _series = Array.Empty<ISeries>();
    public ISeries[] Series
    {
        get => _series;
        set => SetProperty(ref _series, value);
    }

    private int? _hoveredProductId;
    public int? HoveredProductId
    {
        get => _hoveredProductId;
        set
        {
            if (SetProperty(ref _hoveredProductId, value))
            {
                UpdateSeriesThickness();
            }
        }
    }

    private void UpdateSeriesThickness()
    {
        if (Series == null) return;
        foreach (var s in Series)
        {
            if (s is LineSeries<double> ls)
            {
                var productName = Rows.FirstOrDefault(r => r.ProductId == HoveredProductId)?.ProductName;
                var isHovered = HoveredProductId != null && productName != null && ls.Name == productName;
                
                if (ls.Stroke is SolidColorPaint scp)
                {
                    ls.Stroke = new SolidColorPaint(scp.Color) { StrokeThickness = isHovered ? 10 : 4 };
                }
            }
            else if (s is ScatterSeries<double?> ss)
            {
                var productName = Rows.FirstOrDefault(r => r.ProductId == HoveredProductId)?.ProductName;
                var isHovered = HoveredProductId != null && productName != null && ss.Name != null && ss.Name.StartsWith(productName);
                ss.GeometrySize = isHovered ? 10 : 6;
            }
        }
    }

    private LiveChartsCore.SkiaSharpView.Axis[] _xAxes = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.Axis[] XAxes
    {
        get => _xAxes;
        set => SetProperty(ref _xAxes, value);
    }

    private int? _initialProductId;
    public void SetInitialProductId(int? productId)
    {
        _initialProductId = productId;
    }

    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _seriesCts;

    public ICommand ClearProductCommand { get; }
    public ICommand ResetViewCommand { get; }

    public StockMovementViewModel(ApiClient api)
    {
        _api = api;

        _ = InitializeAsync();

        Pagination.PageSize = 20;
        Pagination.PageChanged += (_, __) =>
        {
            ApplyPaging();
            _ = RefreshSeriesAsync();
        };

        // Default last month range.
        FromDate = DateTimeOffset.Now.AddDays(-30);
        ToDate = DateTimeOffset.Now;
        FromTime = new TimeSpan(0, 0, 0);
        ToTime = new TimeSpan(DateTime.Now.Hour, (DateTime.Now.Minute / 5) * 5, 0);

        // Default view: last month -> now.
        AllHistory = false;
        FromDay0 = false;
        ToNow = true;

        ClearProductCommand = new RelayCommand(() =>
        {
            SelectedProduct = null;
            SelectedProductRow = null;
            SelectedProductGroup = ProductGroups.FirstOrDefault(g => g.ProductGroupId == 0);
            ProductSearchText = string.Empty;
            SelectedProductLetter = "ALL";
        });

        ResetViewCommand = new RelayCommand(() =>
        {
            SelectedProductRow = null;
            _ = RefreshSeriesAsync();
        });
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
            var groups = await _api.GetAsync<List<MetalLink.Shared.Products.ProductGroupDto>>("api/products/groups") ?? new List<MetalLink.Shared.Products.ProductGroupDto>();
            ProductGroups.Clear();
            var allGroup = new MetalLink.Shared.Products.ProductGroupDto { ProductGroupId = 0, ProductGroupName = "All" };
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

        Rows = new ObservableCollection<StockLevelLookupDto>(page);
        Console.WriteLine($"[DEBUG] StockMovementViewModel: ApplyPaging updated Rows with {Rows.Count} items. skip={skip}, take={take}");
        OnPropertyChanged(nameof(PaginationStatusText));
    }

    public async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var ct = _refreshCts.Token;

        try
        {
            var term = string.IsNullOrWhiteSpace(ProductSearchText) ? null : ProductSearchText.Trim();
            var letter = string.IsNullOrWhiteSpace(SelectedProductLetter) ? null : SelectedProductLetter.Trim();
            var groupId = SelectedProductGroup?.ProductGroupId ?? 0;

            // Reuse stock-level lookup as product search source.
            // Rule: only starred products (showNonStarred = false)
            var url = $"api/stock-levels/lookup?take=500&includeNonStarred=false";
            if (!string.IsNullOrWhiteSpace(term))
                url += $"&term={Uri.EscapeDataString(term)}";
            if (groupId > 0)
                url += $"&groupId={groupId}";
            if (!string.IsNullOrWhiteSpace(letter) && letter != "ALL")
                url += $"&letter={Uri.EscapeDataString(letter)}";

            var results = await _api.GetAsync<StockLevelLookupDto[]>(url, ct) ?? Array.Empty<StockLevelLookupDto>();
            var distinctResults = results
                .GroupBy(r => r.ProductId)
                .Select(g => g.First())
                .ToArray();

            Console.WriteLine($"[DEBUG] StockMovementViewModel: RefreshAsync returned {results.Length} products from {url}, reduced to {distinctResults.Length} unique products.");
            foreach(var r in distinctResults.Take(3)) {
                Console.WriteLine($"  - Product: {r.ProductName}, Code: {r.ProductCode}");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine($"[DEBUG] StockMovementViewModel: Dispatching UI update. Results count: {distinctResults.Length}");
                // Ensure results and chart are expanded on load/refresh
                IsChartExpanded = true;
                IsResultsExpanded = true;

                ProductSuggestions = new ObservableCollection<StockLevelLookupDto>(distinctResults);
                Console.WriteLine($"[DEBUG] StockMovementViewModel: Updated ProductSuggestions with {ProductSuggestions.Count} items.");

                // If we were navigated from Stock Levels, preselect the product once suggestions load.
                if (_initialProductId is not null && SelectedProduct is null)
                {
                    SelectedProduct = results.FirstOrDefault(r => r.ProductId == _initialProductId.Value);
                    _initialProductId = null; // consume
                }

                var rows = SelectedProduct is null
                    ? results
                    : results.Where(r => r.ProductId == SelectedProduct.ProductId).ToArray();

                _allRows = new ObservableCollection<StockLevelLookupDto>(rows);
                Pagination.SetTotalRecords(_allRows.Count);
                Console.WriteLine($"[DEBUG] StockMovementViewModel: Total records set to {_allRows.Count}.");
                ApplyPaging();
                Console.WriteLine($"[DEBUG] StockMovementViewModel: Applied paging. Current rows count: {Rows.Count}.");
            });

            await RefreshSeriesAsync();
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockMovement] Refresh failed: {ex.Message}");
        }
    }

    private async Task RefreshSeriesAsync()
    {
        _seriesCts?.Cancel();
        _seriesCts = new CancellationTokenSource();
        var ct = _seriesCts.Token;

        try
        {
            var pageProductIds = Rows.Select(r => r.ProductId).Distinct().ToArray();
            if (pageProductIds.Length == 0)
            {
                Series = Array.Empty<ISeries>();
                Transactions = new();
                return;
            }

            var req = new StockMovementTimeSeriesRequestDto
            {
                ProductIds = pageProductIds,
                AllHistory = AllHistory,
                FromDay0 = FromDay0,
                ToNow = ToNow,
                From = (AllHistory || FromDay0) ? null : BuildFromDateTimeOffset(),
                To = (AllHistory || ToNow) ? null : BuildToDateTimeOffset(),
                BucketCount = null
            };

            var resp = await _api.PostAsync<StockMovementTimeSeriesRequestDto, StockMovementTimeSeriesResponseDto>(
                "api/stock-movements/time-series",
                req,
                ct);

            if (resp is null) return;

            // Filter products if one is selected in Movement History
            var displayProducts = SelectedProductRow == null
                ? resp.Products
                : resp.Products.Where(p => p.ProductId == SelectedProductRow.ProductId).ToArray();

            // Build Transactions List
            var transactions = new List<TransactionViewModel>();
            foreach (var p in displayProducts)
            {
                var productName = Rows.FirstOrDefault(r => r.ProductId == p.ProductId)?.ProductName ?? $"Product {p.ProductId}";
                for (int i = 1; i < p.Points.Length; i++)
                {
                    var prev = p.Points[i - 1];
                    var curr = p.Points[i];
                    if (curr.LevelKg != prev.LevelKg)
                    {
                        transactions.Add(new TransactionViewModel
                        {
                            ProductId = p.ProductId,
                            Timestamp = curr.Time,
                            ProductName = productName,
                            Quantity = curr.LevelKg - prev.LevelKg,
                            OnHandAfter = curr.LevelKg,
                            TypeLabel = (curr.LevelKg - prev.LevelKg) >= 0 ? "Buy" : "Sell"
                        });
                    }
                }
            }

            // Build axis labels from bucket ends
            var labels = resp.Products.FirstOrDefault()?.Points
                ?.Select(p => FormatBucketLabel(resp.From, resp.To, p.Time))
                .ToArray() ?? Array.Empty<string>();

            var xAxes = new[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = labels,
                    LabelsRotation = 0,
                    TextSize = 10,
                }
            };

            // Build LiveCharts series
            var series = new List<ISeries>();
            var palette = new[] { SKColors.DeepSkyBlue, SKColors.OrangeRed, SKColors.LimeGreen, SKColors.Gold, SKColors.MediumPurple, SKColors.HotPink, SKColors.Cyan };

            var colorIndex = 0;
            foreach (var p in displayProducts)
            {
                var name = Rows.FirstOrDefault(r => r.ProductId == p.ProductId)?.ProductName ?? $"Product {p.ProductId}";
                var values = p.Points.Select(pt => (double)pt.LevelKg).ToArray();
                var color = palette[colorIndex++ % palette.Length];

                var lineSeries = new LineSeries<double>
                {
                    Name = name,
                    Values = values,
                    GeometrySize = 0,
                    LineSmoothness = 0,
                    Fill = null,
                    Stroke = new SolidColorPaint(color) { StrokeThickness = 4 },
                    Mapping = (val, index) => new LiveChartsCore.Kernel.Coordinate(index, val),
                    YToolTipLabelFormatter = (point) => {
                        var pt = p.Points[point.Index];
                        var prevPt = point.Index > 0 ? p.Points[point.Index - 1] : null;
                        var change = prevPt != null ? (pt.LevelKg - prevPt.LevelKg) : 0;
                        var typeLabel = (change >= 0) ? "Buy" : "Sell";
                        
                        if (change != 0)
                            return $"On Hand: {pt.LevelKg:N2} {typeLabel}: {Math.Abs(change):N2}";
                        else                        
                            return $"On Hand: {pt.LevelKg:N2}";
                    }
                };

                // Add dots for: Opening Balance (Index 0) and Transactions (Level change)
                var transactionPoints = new List<double?>();
                for(int i = 0; i < p.Points.Length; i++)
                {
                    var pt = p.Points[i];
                    var prevPt = i > 0 ? p.Points[i - 1] : null;
                    
                    if (i == 0 || (prevPt != null && pt.LevelKg != prevPt.LevelKg))
                        transactionPoints.Add((double)pt.LevelKg);
                    else
                        transactionPoints.Add(null);
                }

                var scatterSeries = new ScatterSeries<double?>
                {
                    Name = $"{name}",
                    Values = transactionPoints,
                    GeometrySize = 6,
                    Stroke = null,
                    Fill = new SolidColorPaint(color),
                    IsVisibleAtLegend = false,
                    YToolTipLabelFormatter = (point) => {
                        var pt = p.Points[point.Index];
                        var prevPt = point.Index > 0 ? p.Points[point.Index - 1] : null;
                        var change = prevPt != null ? (pt.LevelKg - prevPt.LevelKg) : 0;
                        var typeLabel = (change >= 0) ? "Buy" : "Sell";
                        
                        if (change != 0)
                            return $"On Hand: {pt.LevelKg:N2} {typeLabel}: {Math.Abs(change):N2}";
                        else
                            return $"On Hand: {pt.LevelKg:N2}";
                    }
                };

                series.Add(lineSeries);
                series.Add(scatterSeries);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                XAxes = xAxes;
                Series = series.ToArray();
                Transactions = new ObservableCollection<TransactionViewModel>(transactions.OrderByDescending(t => t.Timestamp));
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[StockMovement] Series refresh failed: {ex.Message}");
        }
    }

    private static string FormatBucketLabel(DateTimeOffset from, DateTimeOffset to, DateTimeOffset t)
    {
        var rangeDays = (to - from).TotalDays;
        if (rangeDays <= 31)
            return t.ToLocalTime().ToString("dd MMM");
        if (rangeDays <= 120)
            return t.ToLocalTime().ToString("dd MMM");
        return t.ToLocalTime().ToString("MMM yy");
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
