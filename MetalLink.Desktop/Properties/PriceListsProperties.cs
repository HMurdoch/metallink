using System.Collections.ObjectModel;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.ViewModels;


public partial class MainWindowViewModel
{
    private ObservableCollection<ProductPriceListDto> _priceListsResults = new();
    public ObservableCollection<ProductPriceListDto> PriceListsResults
    {
        get => _priceListsResults;
        set { _priceListsResults = value; OnPropertyChanged(); }
    }

    private ProductPriceListDto? _selectedPriceList;
    public ProductPriceListDto? SelectedPriceList
    {
        get => _selectedPriceList;
        set 
        { 
            _selectedPriceList = value; 
            OnPropertyChanged();
            if (value != null) OnEditPriceList(value);
        }
    }

    private string _priceListSearchTerm = string.Empty;
    public string PriceListSearchTerm
    {
        get => _priceListSearchTerm;
        set 
        { 
            _priceListSearchTerm = value; 
            OnPropertyChanged();
            _ = SearchPriceListsAsync();
        }
    }

    private string _selectedPriceListEntityType = "Customer";
    public string SelectedPriceListEntityType
    {
        get => _selectedPriceListEntityType;
        set 
        { 
            _selectedPriceListEntityType = value; 
            OnPropertyChanged();
            _ = SearchPriceListsAsync();
        }
    }

    private bool _priceListsIsSearchCriteriaExpanded = true;
    public bool PriceListsIsSearchCriteriaExpanded
    {
        get => _priceListsIsSearchCriteriaExpanded;
        set { _priceListsIsSearchCriteriaExpanded = value; OnPropertyChanged(); }
    }

    private bool _priceListsIsSearchResultsExpanded = true;
    public bool PriceListsIsSearchResultsExpanded
    {
        get => _priceListsIsSearchResultsExpanded;
        set { _priceListsIsSearchResultsExpanded = value; OnPropertyChanged(); }
    }

    private bool _priceListsIsCreateEditExpanded = false;
    public bool PriceListsIsCreateEditExpanded
    {
        get => _priceListsIsCreateEditExpanded;
        set { _priceListsIsCreateEditExpanded = value; OnPropertyChanged(); }
    }

    // Create/Edit form properties
    private int? _editingPriceListId;
    public int? EditingPriceListId
    {
        get => _editingPriceListId;
        set { _editingPriceListId = value; OnPropertyChanged(); }
    }

    private string _priceListName = string.Empty;
    public string PriceListName
    {
        get => _priceListName;
        set { _priceListName = value; OnPropertyChanged(); }
    }

    private string? _priceListDescription;
    public string? PriceListDescription
    {
        get => _priceListDescription;
        set { _priceListDescription = value; OnPropertyChanged(); }
    }

    private string _priceListEntityType = "Customer";
    public string PriceListEntityType
    {
        get => _priceListEntityType;
        set
        {
            _priceListEntityType = value;
            OnPropertyChanged();
            _ = LoadCloneFromListAsync();
        }
    }

    public bool IsPriceListEditMode => EditingPriceListId.HasValue;
    public bool IsPriceListCreateMode => !EditingPriceListId.HasValue;
    public string PriceListSaveButtonText => IsPriceListEditMode ? "Update" : "Create";

    // Seeding options (only relevant in Create mode)
    private bool _useLastKnownPrice = true;
    public bool UseLastKnownPrice
    {
        get => _useLastKnownPrice;
        set
        {
            _useLastKnownPrice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCloneDropdownEnabled));
        }
    }

    private ProductPriceListDto? _selectedCloneFrom;
    public ProductPriceListDto? SelectedCloneFrom
    {
        get => _selectedCloneFrom;
        set { _selectedCloneFrom = value; OnPropertyChanged(); }
    }

    private ObservableCollection<ProductPriceListDto> _cloneFromPriceLists = new();
    public ObservableCollection<ProductPriceListDto> CloneFromPriceLists
    {
        get => _cloneFromPriceLists;
        set { _cloneFromPriceLists = value; OnPropertyChanged(); }
    }

    public bool IsCloneDropdownEnabled => !UseLastKnownPrice && IsPriceListCreateMode;
}
