using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Buyers;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private string _searchBuyerIdText = string.Empty;
    public string SearchBuyerIdText { get => _searchBuyerIdText; set { _searchBuyerIdText = value; OnPropertyChanged(); } }

    private string _searchBuyerFirstNameText = string.Empty;
    public string SearchBuyerFirstNameText { get => _searchBuyerFirstNameText; set { _searchBuyerFirstNameText = value; OnPropertyChanged(); } }

    private string _searchBuyerLastNameText = string.Empty;
    public string SearchBuyerLastNameText { get => _searchBuyerLastNameText; set { _searchBuyerLastNameText = value; OnPropertyChanged(); } }

    private string _searchBuyerCompanyNameText = string.Empty;
    public string SearchBuyerCompanyNameText { get => _searchBuyerCompanyNameText; set { _searchBuyerCompanyNameText = value; OnPropertyChanged(); } }

    private string _searchBuyerIdNumberText = string.Empty;
    public string SearchBuyerIdNumberText { get => _searchBuyerIdNumberText; set { _searchBuyerIdNumberText = value; OnPropertyChanged(); } }

    private string _searchBuyerAccountNumberText = string.Empty;
    public string SearchBuyerAccountNumberText { get => _searchBuyerAccountNumberText; set { _searchBuyerAccountNumberText = value; OnPropertyChanged(); } }

    private string _searchBuyerPhoneNumberText = string.Empty;
    public string SearchBuyerPhoneNumberText { get => _searchBuyerPhoneNumberText; set { _searchBuyerPhoneNumberText = value; OnPropertyChanged(); } }

    private string _searchBuyerMobileNumberText = string.Empty;
    public string SearchBuyerMobileNumberText { get => _searchBuyerMobileNumberText; set { _searchBuyerMobileNumberText = value; OnPropertyChanged(); } }

    private string _searchBuyerEmailText = string.Empty;
    public string SearchBuyerEmailText { get => _searchBuyerEmailText; set { _searchBuyerEmailText = value; OnPropertyChanged(); } }

    private ProductPriceListDto? _searchBuyerPriceList;
    public ProductPriceListDto? SearchBuyerPriceList { get => _searchBuyerPriceList; set { _searchBuyerPriceList = value; OnPropertyChanged(); } }

    private ObservableCollection<BuyerDto> _buyerSearchResults = new();
    public ObservableCollection<BuyerDto> BuyerSearchResults
    {
        get => _buyerSearchResults;
        set { _buyerSearchResults = value; OnPropertyChanged(); }
    }

    private ObservableCollection<BuyerDto> _pagedBuyerSearchResults = new();
    public ObservableCollection<BuyerDto> PagedBuyerSearchResults
    {
        get => _pagedBuyerSearchResults;
        set { _pagedBuyerSearchResults = value; OnPropertyChanged(); }
    }

    private ObservableCollection<ProductPriceListDto> _buyerPriceLists = new();
    public ObservableCollection<ProductPriceListDto> BuyerPriceLists
    {
        get => _buyerPriceLists;
        set { _buyerPriceLists = value; OnPropertyChanged(); }
    }

    public string FoundBuyerSummary => FoundBuyer == null ? "No buyer loaded." : $"ID: {FoundBuyer.BuyerId:D8}, Name: {FoundBuyer.FirstName} {FoundBuyer.LastName}";
    public string SelectedBuyerIdDisplay => FoundBuyer == null ? string.Empty : FoundBuyer.BuyerId.ToString("D8");
}
