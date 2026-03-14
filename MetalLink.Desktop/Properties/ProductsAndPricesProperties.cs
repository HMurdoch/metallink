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
            ApplyProductLetterFilter();
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

    private long? _editingProductId;
    public long? EditingProductId
    {
        get => _editingProductId;
        set { _editingProductId = value; OnPropertyChanged(); }
    }

    private string _productName = string.Empty;
    public string ProductName
    {
        get => _productName;
        set 
        { 
            _productName = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateProduct));
            OnPropertyChanged(nameof(CanUpdateProduct));
            (CreateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string _productCode = string.Empty;
    public string ProductCode
    {
        get => _productCode;
        set 
        { 
            _productCode = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateProduct));
            OnPropertyChanged(nameof(CanUpdateProduct));
            (CreateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateProductCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private decimal? _productGrade = 0;
    public decimal? ProductGrade
    {
        get => _productGrade;
        set { _productGrade = value; OnPropertyChanged(); }
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

    private long? _editingPriceId;
    public long? EditingPriceId
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
