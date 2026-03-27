using System.Collections.ObjectModel;
using MetalLink.Shared.Products;
using MetalLink.Shared.Prices;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using CommunityToolkit.Mvvm.Input;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{

    // =====================================================
    // PRODUCT SEARCH & LETTER FILTER
    // =====================================================

    private ObservableCollection<ProductGroupDto> _productGroups = new();
    public ObservableCollection<ProductGroupDto> ProductGroups
    {
        get => _productGroups;
        set { _productGroups = value; OnPropertyChanged(); }
    }

    private ProductGroupDto? _selectedProductGroup;
    public ProductGroupDto? SelectedProductGroup
    {
        get => _selectedProductGroup;
        set 
        { 
            _selectedProductGroup = value; 
            OnPropertyChanged();
            _ = ApplyProductFiltersAsync();
        }
    }

    private bool _showNonStarred = false;
    public bool ShowNonStarred
    {
        get => _showNonStarred;
        set 
        { 
            _showNonStarred = value; 
            OnPropertyChanged();
            _ = ApplyProductFiltersAsync();
        }
    }

    private string _productSearchTerm = string.Empty;
    public string ProductSearchTerm
    {
        get => _productSearchTerm;
        set
        {
            _productSearchTerm = value;
            OnPropertyChanged();
            _ = ApplyProductFiltersAsync();
        }
    }

    private ObservableCollection<string> _productLetterFilters = new();
    public ObservableCollection<string> ProductLetterFilters
    {
        get => _productLetterFilters;
        set { _productLetterFilters = value; OnPropertyChanged(); }
    }

    private string? _selectedProductLetter = "ALL";
    public string? SelectedProductLetter
    {
        get => _selectedProductLetter;
        set 
        { 
            _selectedProductLetter = value; 
            OnPropertyChanged();
            _ = ApplyProductFiltersAsync();
        }
    }

    private ObservableCollection<ProductLookupDto> _searchProductSuggestions = new();
    public ObservableCollection<ProductLookupDto> SearchProductSuggestions
    {
        get => _searchProductSuggestions;
        set { _searchProductSuggestions = value; OnPropertyChanged(); }
    }

    private ProductLookupDto? _selectedSearchProduct;
    public ProductLookupDto? SelectedSearchProduct
    {
        get => _selectedSearchProduct;
        set { _selectedSearchProduct = value; OnPropertyChanged(); }
    }

    // =====================================================
    // PRODUCT RESULTS GRID
    // =====================================================

    private ObservableCollection<ProductLookupDto> _productResults = new();
    public ObservableCollection<ProductLookupDto> ProductResults
    {
        get => _productResults;
        set { _productResults = value; OnPropertyChanged(); }
    }

    private ProductLookupDto? _selectedProduct;
    public ProductLookupDto? SelectedProduct
    {
        get => _selectedProduct;
        set 
        { 
            _selectedProduct = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdatePrice));
            (UpdatePriceCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            _ = LoadPricesForSelectedProductAsync();
        }
    }

    // =====================================================
    // PRODUCT CREATE/EDIT FORM
    // =====================================================

    private int? _editingProductId;
    public int? EditingProductId
    {
        get => _editingProductId;
        set { _editingProductId = value; OnPropertyChanged(); }
    }

    private string? _productHtsCode;
    public string? ProductHtsCode
    {
        get => _productHtsCode;
        set { _productHtsCode = value; OnPropertyChanged(); }
    }

    private bool _productIsIsri;
    public bool ProductIsIsri
    {
        get => _productIsIsri;
        set { _productIsIsri = value; OnPropertyChanged(); }
    }

    private string _productIsriCode = string.Empty;
    public string ProductIsriCode
    {
        get => _productIsriCode;
        set { _productIsriCode = value; OnPropertyChanged(); }
    }

    private string _productIsriName = string.Empty;
    public string ProductIsriName
    {
        get => _productIsriName;
        set { _productIsriName = value; OnPropertyChanged(); }
    }

    private string? _productIsriDescription;
    public string? ProductIsriDescription
    {
        get => _productIsriDescription;
        set { _productIsriDescription = value; OnPropertyChanged(); }
    }

    private string? _productIsriUrl;
    public string? ProductIsriUrl
    {
        get => _productIsriUrl;
        set { _productIsriUrl = value; OnPropertyChanged(); }
    }

    private int _productGroupId;
    public int ProductGroupId
    {
        get => _productGroupId;
        set { _productGroupId = value; OnPropertyChanged(); }
    }

    private int _productSpecFlagId;
    public int ProductSpecFlagId
    {
        get => _productSpecFlagId;
        set { _productSpecFlagId = value; OnPropertyChanged(); }
    }

    private bool _productStarred;
    public bool ProductStarred
    {
        get => _productStarred;
        set { _productStarred = value; OnPropertyChanged(); }
    }

    private string? _productStarredAlias;
    public string? ProductStarredAlias
    {
        get => _productStarredAlias;
        set { _productStarredAlias = value; OnPropertyChanged(); }
    }

    private bool _productMustDeclare;
    public bool ProductMustDeclare
    {
        get => _productMustDeclare;
        set { _productMustDeclare = value; OnPropertyChanged(); }
    }

    // =====================================================
    // PRODUCT PRICE LISTS
    // =====================================================

    private ObservableCollection<ProductPriceListDto> _productPriceLists = new();
    public ObservableCollection<ProductPriceListDto> ProductPriceLists
    {
        get => _productPriceLists;
        set { _productPriceLists = value; OnPropertyChanged(); }
    }

    private ProductPriceListDto? _selectedProductPriceList;
    public ProductPriceListDto? SelectedProductPriceList
    {
        get => _selectedProductPriceList;
        set 
        { 
            _selectedProductPriceList = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdatePrice));
            (UpdatePriceCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    // =====================================================
    // PRICE CREATE/EDIT FORM
    // =====================================================

    private int? _editingPriceId;
    public int? EditingPriceId
    {
        get => _editingPriceId;
        set { _editingPriceId = value; OnPropertyChanged(); }
    }

    private decimal _currentPrice;
    public decimal CurrentPrice
    {
        get => _currentPrice;
        set { _currentPrice = value; OnPropertyChanged(); }
    }

    // Legacy price properties kept for compatibility during migration if needed
    public decimal PriceA { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
}
