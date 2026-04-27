using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Stock;
using System.Collections.ObjectModel;
using System.Linq;
using System;
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

    private readonly ApiClient _apiClient;

    public PriceListStockMovementsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadPriceLists();
        LoadData();
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
                null,
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
    private void ApplyFilters()
    {
        CurrentPage = 1;
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