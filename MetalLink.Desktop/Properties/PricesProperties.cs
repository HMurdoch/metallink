using System.Collections.ObjectModel;
using MetalLink.Shared.Prices;
using MetalLink.Shared.Products;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // -------------------------------------------------------
    // All price lists loaded for the current entity type
    // -------------------------------------------------------
    // (private backing store – public access via Available lists)

    // -------------------------------------------------------
    // Price List Selection – 4 available option sets
    // -------------------------------------------------------

    private ObservableCollection<ProductPriceListDto> _pricesAvailableList1 = new();
    public ObservableCollection<ProductPriceListDto> PricesAvailableList1
    {
        get => _pricesAvailableList1;
        set { _pricesAvailableList1 = value; OnPropertyChanged(); }
    }

    private ObservableCollection<ProductPriceListDto> _pricesAvailableList2 = new();
    public ObservableCollection<ProductPriceListDto> PricesAvailableList2
    {
        get => _pricesAvailableList2;
        set { _pricesAvailableList2 = value; OnPropertyChanged(); }
    }

    private ObservableCollection<ProductPriceListDto> _pricesAvailableList3 = new();
    public ObservableCollection<ProductPriceListDto> PricesAvailableList3
    {
        get => _pricesAvailableList3;
        set { _pricesAvailableList3 = value; OnPropertyChanged(); }
    }

    private ObservableCollection<ProductPriceListDto> _pricesAvailableList4 = new();
    public ObservableCollection<ProductPriceListDto> PricesAvailableList4
    {
        get => _pricesAvailableList4;
        set { _pricesAvailableList4 = value; OnPropertyChanged(); }
    }

    // -------------------------------------------------------
    // Selected price lists (one per column)
    // -------------------------------------------------------

    private ProductPriceListDto? _selectedPricesList1;
    public ProductPriceListDto? SelectedPricesList1
    {
        get => _selectedPricesList1;
        set
        {
            if (_selectedPricesList1 == value) return;
            _selectedPricesList1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPriceColumn1Visible));
            OnPropertyChanged(nameof(PriceColumn1Header));
            RefreshAvailablePriceLists();
            _ = LoadPriceColumnAsync(1);
        }
    }

    private ProductPriceListDto? _selectedPricesList2;
    public ProductPriceListDto? SelectedPricesList2
    {
        get => _selectedPricesList2;
        set
        {
            if (_selectedPricesList2 == value) return;
            _selectedPricesList2 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPriceColumn2Visible));
            OnPropertyChanged(nameof(PriceColumn2Header));
            RefreshAvailablePriceLists();
            _ = LoadPriceColumnAsync(2);
        }
    }

    private ProductPriceListDto? _selectedPricesList3;
    public ProductPriceListDto? SelectedPricesList3
    {
        get => _selectedPricesList3;
        set
        {
            if (_selectedPricesList3 == value) return;
            _selectedPricesList3 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPriceColumn3Visible));
            OnPropertyChanged(nameof(PriceColumn3Header));
            RefreshAvailablePriceLists();
            _ = LoadPriceColumnAsync(3);
        }
    }

    private ProductPriceListDto? _selectedPricesList4;
    public ProductPriceListDto? SelectedPricesList4
    {
        get => _selectedPricesList4;
        set
        {
            if (_selectedPricesList4 == value) return;
            _selectedPricesList4 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPriceColumn4Visible));
            OnPropertyChanged(nameof(PriceColumn4Header));
            RefreshAvailablePriceLists();
            _ = LoadPriceColumnAsync(4);
        }
    }

    // -------------------------------------------------------
    // Column visibility (computed)
    // -------------------------------------------------------

    public bool IsPriceColumn1Visible => _selectedPricesList1 != null;
    public bool IsPriceColumn2Visible => _selectedPricesList2 != null;
    public bool IsPriceColumn3Visible => _selectedPricesList3 != null;
    public bool IsPriceColumn4Visible => _selectedPricesList4 != null;

    // -------------------------------------------------------
    // Column headers (computed)
    // -------------------------------------------------------

    public string PriceColumn1Header => _selectedPricesList1?.ProductPriceListName ?? "Price List 1";
    public string PriceColumn2Header => _selectedPricesList2?.ProductPriceListName ?? "Price List 2";
    public string PriceColumn3Header => _selectedPricesList3?.ProductPriceListName ?? "Price List 3";
    public string PriceColumn4Header => _selectedPricesList4?.ProductPriceListName ?? "Price List 4";

    // -------------------------------------------------------
    // Entity type
    // -------------------------------------------------------

    private string _pricesEntityType = "Customer";
    public string PricesEntityType
    {
        get => _pricesEntityType;
        set
        {
            if (_pricesEntityType == value) return;
            _pricesEntityType = value;
            OnPropertyChanged();
            _ = LoadPriceListsForEntityTypeAsync();
        }
    }

    // -------------------------------------------------------
    // Product filters (independent from Products page)
    // -------------------------------------------------------

    private ObservableCollection<ProductGroupDto> _pricesProductGroups = new();
    public ObservableCollection<ProductGroupDto> PricesProductGroups
    {
        get => _pricesProductGroups;
        set { _pricesProductGroups = value; OnPropertyChanged(); }
    }

    private ProductGroupDto? _selectedPricesProductGroup;
    public ProductGroupDto? SelectedPricesProductGroup
    {
        get => _selectedPricesProductGroup;
        set
        {
            if (_selectedPricesProductGroup == value) return;
            _selectedPricesProductGroup = value;
            OnPropertyChanged();
            _ = LoadPricesGridAsync();
        }
    }

    private string _pricesSearchTerm = string.Empty;
    public string PricesSearchTerm
    {
        get => _pricesSearchTerm;
        set
        {
            if (_pricesSearchTerm == value) return;
            _pricesSearchTerm = value;
            OnPropertyChanged();
            if (!string.IsNullOrEmpty(value) && _selectedPricesLetter != "ALL")
            {
                _selectedPricesLetter = "ALL";
                OnPropertyChanged(nameof(SelectedPricesLetter));
            }
            _ = LoadPricesGridAsync();
        }
    }

    private ObservableCollection<string> _pricesLetterFilters = new();
    public ObservableCollection<string> PricesLetterFilters
    {
        get => _pricesLetterFilters;
        set { _pricesLetterFilters = value; OnPropertyChanged(); }
    }

    private string? _selectedPricesLetter = "ALL";
    public string? SelectedPricesLetter
    {
        get => _selectedPricesLetter;
        set
        {
            if (_selectedPricesLetter == value) return;
            _selectedPricesLetter = value;
            OnPropertyChanged();
            if (value != "ALL" && !string.IsNullOrEmpty(_pricesSearchTerm))
            {
                _pricesSearchTerm = string.Empty;
                OnPropertyChanged(nameof(PricesSearchTerm));
            }
            _ = LoadPricesGridAsync();
        }
    }

    // -------------------------------------------------------
    // Results grid
    // -------------------------------------------------------

    private ObservableCollection<PriceRowViewModel> _pricesResults = new();
    public ObservableCollection<PriceRowViewModel> PricesResults
    {
        get => _pricesResults;
        set { _pricesResults = value; OnPropertyChanged(); }
    }

    private PaginationViewModel _pricesPagination = new() { PageSize = 50 };
    public PaginationViewModel PricesPagination
    {
        get => _pricesPagination;
        set { _pricesPagination = value; OnPropertyChanged(); }
    }

    // -------------------------------------------------------
    // Collapsible panel states
    // -------------------------------------------------------

    private bool _pricesIsPriceListSelectionExpanded = true;
    public bool PricesIsPriceListSelectionExpanded
    {
        get => _pricesIsPriceListSelectionExpanded;
        set { _pricesIsPriceListSelectionExpanded = value; OnPropertyChanged(); }
    }

    private bool _pricesIsFilterExpanded = true;
    public bool PricesIsFilterExpanded
    {
        get => _pricesIsFilterExpanded;
        set { _pricesIsFilterExpanded = value; OnPropertyChanged(); }
    }

    private bool _pricesIsResultsExpanded = true;
    public bool PricesIsResultsExpanded
    {
        get => _pricesIsResultsExpanded;
        set { _pricesIsResultsExpanded = value; OnPropertyChanged(); }
    }
}
