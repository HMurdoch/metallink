// MetalLink.Desktop/ViewModels/Properties/CustomerProperties.cs
using System;
using System.Collections.ObjectModel;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Locations;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Customer search text fields ---

    private string _searchCustomerIdText = string.Empty;
    private string _searchSiteIdText = string.Empty;
    private string _searchFirstNameText = string.Empty;
    private string _searchLastNameText = string.Empty;
    private string _searchCompanyNameText = string.Empty;
    private string _searchIdNumberText = string.Empty;
    private string _searchPriceCodeText = string.Empty;
    private string _searchAddressLine1Text = string.Empty;
    private string _searchAddressLine2Text = string.Empty;
    private string _searchSuburbText = string.Empty;
    private string _searchCityText = string.Empty;
    private string _searchPostalCodeText = string.Empty;
    private string _searchPhoneNumberText = string.Empty;
    private string _searchMobileNumberText = string.Empty;
    private string _searchEmailText = string.Empty;

    private ObservableCollection<CustomerDto> _customerSearchResults = new();

    // the “selected / loaded” customer that drives the details panel
    private CustomerDto? _foundCustomer;

    private int _totalCustomersInDb;
    private int _totalTicketsInDb;

    // --- New customer form backing fields ---

    // --- Validation / dirty flags ---

    public bool IsNewCustomerFullNameInvalid =>
        string.IsNullOrWhiteSpace(NewFirstName)
        && string.IsNullOrWhiteSpace(NewLastName)
        && string.IsNullOrWhiteSpace(NewCompanyName);

    public bool HasUnsavedNewCustomer =>
        !string.IsNullOrWhiteSpace(NewFirstName)
        || !string.IsNullOrWhiteSpace(NewLastName)
        || NewIsCompany
        || !string.IsNullOrWhiteSpace(NewCompanyName)
        || !string.IsNullOrWhiteSpace(NewIdNumber)
        || NewAccountNumber != null
        || !string.IsNullOrWhiteSpace(NewPriceCode)
        || !string.IsNullOrWhiteSpace(NewAddressLine1)
        || !string.IsNullOrWhiteSpace(NewAddressLine2)
        || !string.IsNullOrWhiteSpace(NewSuburb)
        || !string.IsNullOrWhiteSpace(NewCity)
        || !string.IsNullOrWhiteSpace(NewPostalCode)
        || !string.IsNullOrWhiteSpace(NewPhoneNumber)
        || !string.IsNullOrWhiteSpace(NewMobileNumber)
        || !string.IsNullOrWhiteSpace(NewEmail);

    // --- Customer search properties ---

    public string SearchCustomerIdText
    {
        get => _searchCustomerIdText;
        set { _searchCustomerIdText = value; OnPropertyChanged(); }
    }

    public string SearchSiteIdText
    {
        get => _searchSiteIdText;
        set { _searchSiteIdText = value; OnPropertyChanged(); }
    }

    public string SearchFirstNameText
    {
        get => _searchFirstNameText;
        set { _searchFirstNameText = value; OnPropertyChanged(); }
    }

    public string SearchLastNameText
    {
        get => _searchLastNameText;
        set { _searchLastNameText = value; OnPropertyChanged(); }
    }

    public string SearchCompanyNameText
    {
        get => _searchCompanyNameText;
        set { _searchCompanyNameText = value; OnPropertyChanged(); }
    }

    public string SearchIdNumberText
    {
        get => _searchIdNumberText;
        set { _searchIdNumberText = value; OnPropertyChanged(); }
    }

    public string SearchPriceCodeText
    {
        get => _searchPriceCodeText;
        set { _searchPriceCodeText = value; OnPropertyChanged(); }
    }

    public string SearchAddressLine1Text
    {
        get => _searchAddressLine1Text;
        set { _searchAddressLine1Text = value; OnPropertyChanged(); }
    }

    public string SearchAddressLine2Text
    {
        get => _searchAddressLine2Text;
        set { _searchAddressLine2Text = value; OnPropertyChanged(); }
    }

    public string SearchSuburbText
    {
        get => _searchSuburbText;
        set { _searchSuburbText = value; OnPropertyChanged(); }
    }

    public string SearchCityText
    {
        get => _searchCityText;
        set { _searchCityText = value; OnPropertyChanged(); }
    }

    public string SearchPostalCodeText
    {
        get => _searchPostalCodeText;
        set { _searchPostalCodeText = value; OnPropertyChanged(); }
    }

    public string SearchPhoneNumberText
    {
        get => _searchPhoneNumberText;
        set { _searchPhoneNumberText = value; OnPropertyChanged(); }
    }

    public string SearchMobileNumberText
    {
        get => _searchMobileNumberText;
        set { _searchMobileNumberText = value; OnPropertyChanged(); }
    }

    public string SearchEmailText
    {
        get => _searchEmailText;
        set { _searchEmailText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CustomerDto> CustomerSearchResults
    {
        get => _customerSearchResults;
        set { _customerSearchResults = value; OnPropertyChanged(); }
    }

    public int TotalCustomersInDb
    {
        get => _totalCustomersInDb;
        set { _totalCustomersInDb = value; OnPropertyChanged(); }
    }

    public int TotalTicketsInDb
    {
        get => _totalTicketsInDb;
        set { _totalTicketsInDb = value; OnPropertyChanged(); }
    }

    // --- Loaded / selected customer driving the details panel ---

    public CustomerDto? FoundCustomer
    {
        get => _foundCustomer;
        set
        {
            _foundCustomer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FoundCustomerSummary));
            OnPropertyChanged(nameof(SelectedCustomerIdDisplay));
            OnPropertyChanged(nameof(SelectedFirstName));
            OnPropertyChanged(nameof(SelectedLastName));
            OnPropertyChanged(nameof(SelectedCompanyName));
            OnPropertyChanged(nameof(SelectedSiteName));
            OnPropertyChanged(nameof(SelectedAccountNumberFormatted));
            OnPropertyChanged(nameof(SelectedIdNumber));
            OnPropertyChanged(nameof(SelectedAccountNumber));
            OnPropertyChanged(nameof(SelectedPriceCode));
            OnPropertyChanged(nameof(SelectedAddressLine1));
            OnPropertyChanged(nameof(SelectedAddressLine2));
            OnPropertyChanged(nameof(SelectedSuburb));
            OnPropertyChanged(nameof(SelectedCity));
            OnPropertyChanged(nameof(SelectedPostalCode));
            OnPropertyChanged(nameof(SelectedPhoneNumber));
            OnPropertyChanged(nameof(SelectedMobileNumber));
            OnPropertyChanged(nameof(SelectedEmail));
            OnPropertyChanged(nameof(SelectedProvinceName));
            OnPropertyChanged(nameof(SelectedCountryName));
            OnPropertyChanged(nameof(SelectedTaxable));
        }
    }

    public string FoundCustomerSummary =>
        FoundCustomer == null
            ? "No customer loaded."
            : $"ID: {FoundCustomer.CustomerId:D8}, Name: {FoundCustomer.FirstName} {FoundCustomer.LastName}, Account: {FoundCustomer.AccountNumber}";

    // 8-digit, zero-padded ID
    public string SelectedCustomerIdDisplay =>
        FoundCustomer == null ? string.Empty : FoundCustomer.CustomerId.ToString("D8");

    public string SelectedFirstName      => FoundCustomer?.FirstName ?? "";
    public string SelectedLastName       => FoundCustomer?.LastName ?? "";
    public string SelectedCompanyName    => FoundCustomer?.CompanyName ?? string.Empty;
    public string SelectedSiteName       => FoundCustomer?.SiteName ?? string.Empty;
    public string SelectedAccountNumberFormatted    => FoundCustomer?.AccountNumberFormatted ?? string.Empty;
    public string SelectedIdNumber       => FoundCustomer?.IdNumber ?? string.Empty;
    public long? SelectedAccountNumber  => FoundCustomer?.AccountNumber;
    public string SelectedPriceCode      => FoundCustomer?.PriceCode ?? string.Empty;
    public string SelectedAddressLine1   => FoundCustomer?.AddressLine1 ?? string.Empty;
    public string SelectedAddressLine2   => FoundCustomer?.AddressLine2 ?? string.Empty;
    public string SelectedSuburb         => FoundCustomer?.Suburb ?? string.Empty;
    public string SelectedCity           => FoundCustomer?.City ?? string.Empty;
    public string SelectedPostalCode     => FoundCustomer?.PostalCode ?? string.Empty;
    public string SelectedPhoneNumber    => FoundCustomer?.PhoneNumber ?? string.Empty;
    public string SelectedMobileNumber   => FoundCustomer?.MobileNumber ?? string.Empty;
    public string SelectedEmail          => FoundCustomer?.Email ?? string.Empty;
    public string SelectedProvinceName => FoundCustomer?.ProvinceName ?? string.Empty;
    public string SelectedCountryName  => FoundCustomer?.CountryName  ?? string.Empty;
    public bool   SelectedTaxable      => FoundCustomer?.Taxable ?? false;


    // --- New customer form properties ---


    private string _newFirstName = string.Empty;
    public string NewFirstName
    {
        get => _newFirstName;
        set
        {
            _newFirstName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewCustomerFullNameInvalid));
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private string _newLastName = string.Empty;
    public string NewLastName
    {
        get => _newLastName;
        set
        {
            _newLastName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewCustomerFullNameInvalid));
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private bool _newIsCompany;
    public bool NewIsCompany
    {
        get => _newIsCompany;
        set
        {
            _newIsCompany = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private string? _newCompanyName;
    public string? NewCompanyName
    {
        get => _newCompanyName;
        set
        {
            _newCompanyName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private string? _newIdNumber;
    public string? NewIdNumber
    {
        get => _newIdNumber;
        set
        {
            _newIdNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private long? _newAccountNumber;

    public long? NewAccountNumber
    {
        get => _newAccountNumber;
        private set
        {
            _newAccountNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NewAccountNumberDisplay));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    public string NewAccountNumberDisplay =>
        NewAccountNumber.HasValue ? NewAccountNumber.Value.ToString("D8") : string.Empty;

    public bool IsAccountNumberReadOnly => true;

    private string? _newPriceCode;
    public string? NewPriceCode
    {
        get => _newPriceCode;
        set
        {
            _newPriceCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string _newAddressLine1 = string.Empty;
    public string NewAddressLine1
    {
        get => _newAddressLine1;
        set
        {
            _newAddressLine1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string _newAddressLine2 = string.Empty;
    public string NewAddressLine2
    {
        get => _newAddressLine2;
        set
        {
            _newAddressLine2 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string _newSuburb = string.Empty;
    public string NewSuburb
    {
        get => _newSuburb;
        set
        {
            _newSuburb = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string _newCity = string.Empty;
    public string NewCity
    {
        get => _newCity;
        set
        {
            _newCity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string _newPostalCode = string.Empty;
    public string NewPostalCode
    {
        get => _newPostalCode;
        set
        {
            _newPostalCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string? _newPhoneNumber;
    public string? NewPhoneNumber
    {
        get => _newPhoneNumber;
        set
        {
            _newPhoneNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string? _newMobileNumber;
    public string? NewMobileNumber
    {
        get => _newMobileNumber;
        set
        {
            _newMobileNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string? _newEmail;
    public string? NewEmail
    {
        get => _newEmail;
        set
        {
            _newEmail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private bool _newTaxable = true;
    public bool NewTaxable
    {
        get => _newTaxable;
        set { _newTaxable = value; OnPropertyChanged(); }
    }
    
    private bool _searchTaxable = true;
    public bool SearchTaxable
    {
        get => _searchTaxable;
        set { _searchTaxable = value; OnPropertyChanged(); }
    }
}
