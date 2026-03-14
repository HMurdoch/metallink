using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private string _searchCustomerIdText = string.Empty;
    public string SearchCustomerIdText { get => _searchCustomerIdText; set { _searchCustomerIdText = value; OnPropertyChanged(); } }

    private string _searchCustomerFirstNameText = string.Empty;
    public string SearchCustomerFirstNameText { get => _searchCustomerFirstNameText; set { _searchCustomerFirstNameText = value; OnPropertyChanged(); } }

    private string _searchCustomerLastNameText = string.Empty;
    public string SearchCustomerLastNameText { get => _searchCustomerLastNameText; set { _searchCustomerLastNameText = value; OnPropertyChanged(); } }

    private string _searchCustomerCompanyNameText = string.Empty;
    public string SearchCustomerCompanyNameText { get => _searchCustomerCompanyNameText; set { _searchCustomerCompanyNameText = value; OnPropertyChanged(); } }

    private string _searchCustomerIdNumberText = string.Empty;
    public string SearchCustomerIdNumberText { get => _searchCustomerIdNumberText; set { _searchCustomerIdNumberText = value; OnPropertyChanged(); } }

    private string _searchCustomerAccountNumberText = string.Empty;
    public string SearchCustomerAccountNumberText { get => _searchCustomerAccountNumberText; set { _searchCustomerAccountNumberText = value; OnPropertyChanged(); } }

    private string _searchCustomerPhoneNumberText = string.Empty;
    public string SearchCustomerPhoneNumberText { get => _searchCustomerPhoneNumberText; set { _searchCustomerPhoneNumberText = value; OnPropertyChanged(); } }

    private string _searchCustomerMobileNumberText = string.Empty;
    public string SearchCustomerMobileNumberText { get => _searchCustomerMobileNumberText; set { _searchCustomerMobileNumberText = value; OnPropertyChanged(); } }

    private string _searchCustomerEmailText = string.Empty;
    public string SearchCustomerEmailText { get => _searchCustomerEmailText; set { _searchCustomerEmailText = value; OnPropertyChanged(); } }

    private ProductPriceListDto? _searchCustomerPriceList;
    public ProductPriceListDto? SearchCustomerPriceList { get => _searchCustomerPriceList; set { _searchCustomerPriceList = value; OnPropertyChanged(); } }

    private ObservableCollection<CustomerDto> _customerSearchResults = new();
    public ObservableCollection<CustomerDto> CustomerSearchResults
    {
        get => _customerSearchResults;
        set { _customerSearchResults = value; OnPropertyChanged(); }
    }

    private ObservableCollection<CustomerDto> _pagedCustomerSearchResults = new();
    public ObservableCollection<CustomerDto> PagedCustomerSearchResults
    {
        get => _pagedCustomerSearchResults;
        set { _pagedCustomerSearchResults = value; OnPropertyChanged(); }
    }

    private ObservableCollection<ProductPriceListDto> _customerPriceLists = new();
    public ObservableCollection<ProductPriceListDto> CustomerPriceLists
    {
        get => _customerPriceLists;
        set { _customerPriceLists = value; OnPropertyChanged(); }
    }

    public string FoundCustomerSummary => FoundCustomer == null ? "No customer loaded." : $"ID: {FoundCustomer.CustomerId:D8}, Name: {FoundCustomer.FirstName} {FoundCustomer.LastName}";
    public string SelectedCustomerIdDisplay => FoundCustomer == null ? string.Empty : FoundCustomer.CustomerId.ToString("D8");
}
