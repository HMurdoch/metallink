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

    // =====================================================
    // PRICE CREATE/EDIT FORM
    // =====================================================

    private long? _editingPriceId;
    public long? EditingPriceId
    {
        get => _editingPriceId;
        set { _editingPriceId = value; OnPropertyChanged(); }
    }

    private decimal _priceA;
    public decimal PriceA
    {
        get => _priceA;
        set { _priceA = value; OnPropertyChanged(); }
    }

    private decimal _priceB;
    public decimal PriceB
    {
        get => _priceB;
        set { _priceB = value; OnPropertyChanged(); }
    }

    private decimal _priceC;
    public decimal PriceC
    {
        get => _priceC;
        set { _priceC = value; OnPropertyChanged(); }
    }
}
