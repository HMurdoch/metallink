using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Prices;
using MetalLink.Shared.Products;
using MetalLink.Shared.Stock;

namespace MetalLink.Desktop.ViewModels;

public partial class PriceListStockLevelsViewModel : ViewModelBase
{
    public partial class PriceListSelectionItem : ObservableObject
    {
        [ObservableProperty]
        private int productPriceListId;

        [ObservableProperty]
        private string productPriceListName = string.Empty;

        [ObservableProperty]
        private bool isSelected;
    }

    private bool _isFilterExpanded = true;
    public bool IsFilterExpanded { get => _isFilterExpanded; set => SetProperty(ref _isFilterExpanded, value); }

    private bool _isChartExpanded = true;
    public bool IsChartExpanded { get => _isChartExpanded; set => SetProperty(ref _isChartExpanded, value); }

    private bool _isResultsExpanded = true;
    public bool IsResultsExpanded { get => _isResultsExpanded; set => SetProperty(ref _isResultsExpanded, value); }

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
                _ = RefreshAsync();
        }
    }

    private string _productSearchText = string.Empty;
    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (SetProperty(ref _productSearchText, value))
                _ = RefreshAsync();
        }
    }

    private string _selectedProductLetter = "ALL";
    public string SelectedProductLetter
    {
        get => _selectedProductLetter;
        set
        {
            if (SetProperty(ref _selectedProductLetter, value))
                _ = RefreshAsync();
        }
    }

    public ObservableCollection<string> EntityTypes { get; } = new() { "Customer", "Buyer" };

    private string _selectedEntityType = "Customer";
    public string SelectedEntityType
    {
        get => _selectedEntityType;
        set
        {
            if (SetProperty(ref _selectedEntityType, value))
            {
                _ = LoadPriceListsAsync();
                _ = RefreshAsync();
            }
        }
    }

    public ObservableCollection<PriceListSelectionItem> PriceLists { get; } = new();

    private ObservableCollection<PriceListStockLevelDto> _stockLevels = new();
    public ObservableCollection<PriceListStockLevelDto> StockLevels
    {
        get => _stockLevels;
        set
        {
            if (SetProperty(ref _stockLevels, value))
                OnPropertyChanged(nameof(ChartItems));
        }
    }

    public PaginationViewModel PriceListPagination { get; } = new();

    public string PriceListPaginationStatusText
    {
        get
        {
            if (PriceListPagination.TotalRecords <= 0)
                return "Showing 0 of 0";

            var start = PriceListPagination.GetSkip() + 1;
            var end = Math.Min(PriceListPagination.GetSkip() + PriceListPagination.GetTake(), PriceListPagination.TotalRecords);
            return $"Showing {start}-{end} of {PriceListPagination.TotalRecords}";
        }
    }

    public string CurrentPageInfo => $"Page {PriceListPagination.CurrentPage} of {PriceListPagination.TotalPages}";

    public bool CanGoPrevious => PriceListPagination.CanGoPrevious;
    public bool CanGoNext => PriceListPagination.CanGoNext;

    public IReadOnlyList<MetalLink.Desktop.Views.Controls.BarChart3DControl.BarItem> ChartItems
        => StockLevels
            .Select(r => new MetalLink.Desktop.Views.Controls.BarChart3DControl.BarItem(
                r.ProductId,
                r.ProductName,
                (double)r.TotalWeightKg,
                CreateBrush(r.ProductId)))
            .ToList();

    private Avalonia.Media.IBrush CreateBrush(int productId)
    {
        var hue = (int)(Math.Abs(productId) % 360);
        var color = Avalonia.Media.Color.FromRgb(
            (byte)(80 + (hue % 175)),
            (byte)(60 + ((hue * 3) % 150)),
            (byte)(120 + ((hue * 7) % 120)));
        return new Avalonia.Media.SolidColorBrush(color);
    }

    public ICommand SelectAllPriceListsCommand { get; }
    public ICommand ClearAllPriceListsCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand ApplyFiltersCommand { get; }

    private CancellationTokenSource? _cts;

    public PriceListStockLevelsViewModel(ApiClient api)
    {
        _api = api;
        PriceListPagination.PageSize = 10;
        PriceListPagination.PageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentPageInfo));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            _ = RefreshAsync();
        };

        SelectAllPriceListsCommand = new RelayCommand(SelectAllPriceLists);
        ClearAllPriceListsCommand = new RelayCommand(ClearAllPriceLists);
        PreviousPageCommand = new RelayCommand(PreviousPage, () => PriceListPagination.CanGoPrevious);
        NextPageCommand = new RelayCommand(NextPage, () => PriceListPagination.CanGoNext);
        ApplyFiltersCommand = new RelayCommand(ApplyFilters);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var groups = await _api.GetAsync<List<ProductGroupDto>>("api/products/groups");
            ProductGroups.Clear();
            ProductGroups.Add(new ProductGroupDto { ProductGroupId = 0, ProductGroupName = "All Groups" });
            foreach (var group in groups)
                ProductGroups.Add(group);

            SelectedProductGroup = ProductGroups.First();

            ProductLetterFilters.Clear();
            ProductLetterFilters.Add("ALL");
            for (char c = 'A'; c <= 'Z'; c++)
                ProductLetterFilters.Add(c.ToString());

            await LoadPriceListsAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    private async Task LoadPriceListsAsync()
    {
        try
        {
            var entityFlag = SelectedEntityType == "Customer" ? 'C' : 'B';
            var priceLists = await _api.GetPriceListsAsync(entityFlag);
            PriceLists.Clear();
            foreach (var pl in priceLists)
                PriceLists.Add(new PriceListSelectionItem
                {
                    ProductPriceListId = pl.ProductPriceListId,
                    ProductPriceListName = pl.ProductPriceListName,
                    IsSelected = true
                });

            PriceListPagination.SetTotalRecords(PriceLists.Count);
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    public async Task RefreshAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            var selectedPriceListIds = PriceLists.Where(x => x.IsSelected).Select(x => x.ProductPriceListId).ToArray();
            var stockLevels = await _api.GetPriceListStockLevelsAsync(
                SelectedEntityType == "Customer" ? 'C' : 'B',
                selectedPriceListIds.Length > 0 ? selectedPriceListIds : null,
                SelectedProductGroup?.ProductGroupId > 0 ? SelectedProductGroup.ProductGroupId : null,
                string.IsNullOrWhiteSpace(ProductSearchText) ? null : ProductSearchText,
                SelectedProductLetter,
                PriceListPagination.GetSkip(),
                PriceListPagination.GetTake(),
                ct);

            StockLevels = new ObservableCollection<PriceListStockLevelDto>(stockLevels);
            PriceListPagination.SetTotalRecords(stockLevels.Count);
            OnPropertyChanged(nameof(PriceListPaginationStatusText));
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    private void ApplyFilters()
    {
        PriceListPagination.CurrentPage = 1;
        _ = RefreshAsync();
    }

    private void SelectAllPriceLists()
    {
        foreach (var priceList in PriceLists)
            priceList.IsSelected = true;

        _ = RefreshAsync();
    }

    private void ClearAllPriceLists()
    {
        foreach (var priceList in PriceLists)
            priceList.IsSelected = false;

        _ = RefreshAsync();
    }

    private void PreviousPage()
    {
        if (PriceListPagination.CanGoPrevious)
        {
            PriceListPagination.PreviousPageCommand.Execute(null);
            _ = RefreshAsync();
        }
    }

    private void NextPage()
    {
        if (PriceListPagination.CanGoNext)
        {
            PriceListPagination.NextPageCommand.Execute(null);
            _ = RefreshAsync();
        }
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
