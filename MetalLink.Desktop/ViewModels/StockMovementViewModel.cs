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
using SkiaSharp;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Stock;

namespace MetalLink.Desktop.ViewModels;

public sealed class StockMovementViewModel : ViewModelBase
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
            if (IsFromPickerEnabled) _ = RefreshSeriesAsync();
        }
    }

    private TimeSpan _fromTime = new(0, 0, 0);
    public TimeSpan FromTime
    {
        get => _fromTime;
        set
        {
            if (!SetProperty(ref _fromTime, value)) return;
            if (IsFromPickerEnabled) _ = RefreshSeriesAsync();
        }
    }

    private DateTimeOffset? _toDate = DateTimeOffset.Now;
    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set
        {
            if (!SetProperty(ref _toDate, value)) return;
            if (IsToPickerEnabled) _ = RefreshSeriesAsync();
        }
    }

    private TimeSpan _toTime = new(23, 55, 0);
    public TimeSpan ToTime
    {
        get => _toTime;
        set
        {
            if (!SetProperty(ref _toTime, value)) return;
            if (IsToPickerEnabled) _ = RefreshSeriesAsync();
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

    private LiveChartsCore.SkiaSharpView.Axis[] _xAxes = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
    public LiveChartsCore.SkiaSharpView.Axis[] XAxes
    {
        get => _xAxes;
        set => SetProperty(ref _xAxes, value);
    }

    private long? _initialProductId;
    public void SetInitialProductId(long? productId)
    {
        _initialProductId = productId;
    }

    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _seriesCts;

    public ICommand ClearProductCommand { get; }

    public StockMovementViewModel(ApiClient api)
    {
        _api = api;

        ProductLetterFilters.Add("ALL");
        for (var c = 'A'; c <= 'Z'; c++) ProductLetterFilters.Add(c.ToString());

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
            ProductSearchText = string.Empty;
            SelectedProductLetter = "ALL";
        });
    }

    private void ApplyPaging()
    {
        var skip = Pagination.GetSkip();
        var take = Pagination.GetTake();
        var page = _allRows.Skip(skip).Take(take).ToList();

        Rows = new ObservableCollection<StockLevelLookupDto>(page);
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

            // Reuse stock-level lookup as product search source.
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
                ApplyPaging();
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

            if (resp is null)
                return;

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
                    // Reduce label clutter by showing a subset; LiveCharts will skip some based on SeparatorsPaint.
                    // We can tune this later.
                }
            };

            // Build LiveCharts series
            var series = new List<ISeries>();

            var palette = new[]
            {
                SKColors.DeepSkyBlue,
                SKColors.OrangeRed,
                SKColors.LimeGreen,
                SKColors.Gold,
                SKColors.MediumPurple,
                SKColors.HotPink,
                SKColors.Cyan,
                SKColors.Coral,
                SKColors.Chartreuse,
                SKColors.SandyBrown,
                SKColors.SlateBlue,
                SKColors.Tomato,
                SKColors.YellowGreen,
                SKColors.DodgerBlue,
                SKColors.MediumVioletRed,
                SKColors.Aquamarine,
                SKColors.Peru,
                SKColors.SteelBlue,
                SKColors.OliveDrab,
                SKColors.DeepPink
            };

            var colorIndex = 0;
            foreach (var p in resp.Products)
            {
                var name = Rows.FirstOrDefault(r => r.ProductId == p.ProductId)?.ProductName ?? $"Product {p.ProductId}";

                // X = bucket index (0..N-1), label provided by axis.
                var values = p.Points.Select(pt => (double)pt.LevelKg).ToArray();

                var color = palette[colorIndex++ % palette.Length];

                series.Add(new LineSeries<double>
                {
                    Name = name,
                    Values = values,
                    GeometrySize = 0,
                    LineSmoothness = 0,
                    Fill = null,
                    Stroke = new SolidColorPaint(color) { StrokeThickness = 2 }
                });
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                XAxes = xAxes;
                Series = series.ToArray();
            });
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
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
