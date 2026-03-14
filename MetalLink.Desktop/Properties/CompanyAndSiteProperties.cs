using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Locations;
using MetalLink.Shared.Sites;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Company search inputs
    private string? _companySearchLetter = "ALL";

    public string? CompanySearchLetter
    {
        get => _companySearchLetter;
        set
        {
            _companySearchLetter = value;
            OnPropertyChanged();
        }
    }

    private string _companySearchName = string.Empty;

    public string CompanySearchName
    {
        get => _companySearchName;
        set
        {
            _companySearchName = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    // Company results
    public ObservableCollection<CompanyLookupDto> CompanyResults { get; } = new();

    // Company cache used for letter filtering and dropdowns
    private readonly ObservableCollection<CompanyLookupDto> _allCompanies = new();

    // Dashboard stats for companies/sites/products
    private int _totalCompaniesInDb;
    public int TotalCompaniesInDb
    {
        get => _totalCompaniesInDb;
        set { _totalCompaniesInDb = value; OnPropertyChanged(); }
    }

    private int _totalSitesInDb;
    public int TotalSitesInDb
    {
        get => _totalSitesInDb;
        set { _totalSitesInDb = value; OnPropertyChanged(); }
    }

    private int _totalProductsInDb;
    public int TotalProductsInDb
    {
        get => _totalProductsInDb;
        set { _totalProductsInDb = value; OnPropertyChanged(); }
    }

    private CompanyLookupDto? _selectedCompany;

    public CompanyLookupDto? SelectedCompany
    {
        get => _selectedCompany;
        set
        {
            if (_selectedCompany == value) return;
            _selectedCompany = value;
            OnPropertyChanged();

            ClearCompanyEditor();

            (CreateSiteForSelectedCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanCreateSite));

            SiteResults.Clear();
            SelectedSite = null;

            //CompanyEditName = value?.CompanyName ?? string.Empty;

            if (value != null)
                _ = LoadSitesForSelectedCompanyResultsAsync();
        }
    }

    // Company edit section
    private string _companyEditName = string.Empty;

    public string CompanyEditName
    {
        get => _companyEditName;
        set
        {
            if (_companyEditName == value) return;
            _companyEditName = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateCompany));
            OnPropertyChanged(nameof(CanCreateCompany));

            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (CreateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private long? _editingCompanyId;

    public long? EditingCompanyId
    {
        get => _editingCompanyId;
        set
        {
            _editingCompanyId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCompanyEditMode));
            OnPropertyChanged(nameof(IsCompanyCreateMode));
            OnPropertyChanged(nameof(CanCreateCompany));
            OnPropertyChanged(nameof(CanUpdateCompany));
            (CreateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string _companyFormName = "";

    public string CompanyFormName
    {
        get => _companyFormName;
        set
        {
            _companyFormName = value ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCompany));
            OnPropertyChanged(nameof(CanUpdateCompany));
            (CreateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string _companyFormVatNumber = "";

    public string CompanyFormVatNumber
    {
        get => _companyFormVatNumber;
        set
        {
            _companyFormVatNumber = value ?? "";
            OnPropertyChanged();
        }
    }

    private string _companyFormInitialSiteName = "";

    public string CompanyFormInitialSiteName
    {
        get => _companyFormInitialSiteName;
        set
        {
            _companyFormInitialSiteName = value ?? "";
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCompany));
            (CreateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    // Create/Edit fields
    private string _companyEditVatNumber = string.Empty;

    public string CompanyEditVatNumber
    {
        get => _companyEditVatNumber;
        set
        {
            _companyEditVatNumber = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _newCompanyCreateVatNumber = string.Empty;

    public string NewCompanyCreateVatNumber
    {
        get => _newCompanyCreateVatNumber;
        set
        {
            _newCompanyCreateVatNumber = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCompany));
        }
    }

    // Sites list for selected company
    public ObservableCollection<SiteLookupDto> SiteResults { get; } = new();

    private SiteLookupDto? _selectedSite;

    public SiteLookupDto? SelectedSite
    {
        get => _selectedSite;
        set
        {
            if (_selectedSite == value) return;
            _selectedSite = value;
            OnPropertyChanged();

            // TODO: when selected, load site details into Site edit fields
            LoadSelectedSiteIntoEditFields(value);
        }
    }

    private string _siteFormError = string.Empty;

    public string SiteFormError
    {
        get => _siteFormError;
        set
        {
            _siteFormError = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSiteFormError));
        }
    }

    public bool HasSiteFormError => !string.IsNullOrWhiteSpace(SiteFormError);

    private string _siteFormSuccess = string.Empty;

    public string SiteFormSuccess
    {
        get => _siteFormSuccess;
        set
        {
            _siteFormSuccess = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSiteFormSuccess));
        }
    }

    // Site create/edit fields (remember: address belongs to Site)
    private long? _editingSiteId;

    public long? EditingSiteId
    {
        get => _editingSiteId;
        set
        {
            _editingSiteId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSiteEditMode));
        }
    }

    private string _siteName = string.Empty;

    public string SiteName
    {
        get => _siteName;
        set
        {
            _siteName = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateOrUpdateSite));
            (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string _siteCode = string.Empty;

    public string SiteCode
    {
        get => _siteCode;
        set
        {
            _siteCode = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _addressLine1 = string.Empty;

    public string AddressLine1
    {
        get => _addressLine1;
        set
        {
            _addressLine1 = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _addressLine2 = string.Empty;

    public string AddressLine2
    {
        get => _addressLine2;
        set
        {
            _addressLine2 = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _suburb = string.Empty;

    public string Suburb
    {
        get => _suburb;
        set
        {
            _suburb = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _city = string.Empty;

    public string City
    {
        get => _city;
        set
        {
            _city = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _postalCode = string.Empty;

    public string PostalCode
    {
        get => _postalCode;
        set
        {
            _postalCode = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    // New customer address fields (CAS-owned, used by Customers screen)
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

    private string _newSiteCreateName = string.Empty;

    public string NewSiteCreateName
    {
        get => _newSiteCreateName;
        set
        {
            _newSiteCreateName = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateSite));

            // 🔥 THIS IS THE FIX
            (CreateSiteForSelectedCompanyCommand as IAsyncRelayCommand)
                ?.NotifyCanExecuteChanged();
        }
    }

    private string _newSiteCreateCode = string.Empty;

    public string NewSiteCreateCode
    {
        get => _newSiteCreateCode;
        set
        {
            _newSiteCreateCode = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _newCompanySiteName = string.Empty;

    public string NewCompanySiteName
    {
        get => _newCompanySiteName;
        set
        {
            _newCompanySiteName = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCompany));
        }
    }

    private string _companyVatNumber = string.Empty;

    public string CompanyVatNumber
    {
        get => _companyVatNumber;
        set
        {
            _companyVatNumber = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateCompany));
            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private bool _companyLettersLoaded;
    private bool _companyLettersLoading;
    private readonly ObservableCollection<string> _companyLetterFilters = new();

    public ObservableCollection<string> CompanyLetterFilters
    {
        get
        {
            if (!_companyLettersLoaded && !_companyLettersLoading)
            {
                _companyLettersLoading = true;
                _ = LoadCompaniesAndLettersAsync();
            }

            return _companyLetterFilters;
        }
    }

    private string? _selectedNewCompanyLetter = "ALL";

    public string? SelectedNewCompanyLetter
    {
        get => _selectedNewCompanyLetter;
        set
        {
            if (_selectedNewCompanyLetter == value) return;
            _selectedNewCompanyLetter = value;
            OnPropertyChanged();
            ApplyNewCompanyLetterFilter();
        }
    }

    // --- Company Management Search ---
    private string? _selectedCompanyLetter = "ALL";
    public string? SelectedCompanyLetter
    {
        get => _selectedCompanyLetter;
        set
        {
            if (_selectedCompanyLetter == value) return;
            _selectedCompanyLetter = value;
            OnPropertyChanged();
            SelectedSearchCompany = null;
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
            ApplyCompanyLetterFilter();
        }
    }

    private ObservableCollection<CompanyLookupDto> _searchCompanySuggestions = new();
    public ObservableCollection<CompanyLookupDto> SearchCompanySuggestions
    {
        get => _searchCompanySuggestions;
        set { _searchCompanySuggestions = value; OnPropertyChanged(); }
    }

    private CompanyLookupDto? _selectedSearchCompany;
    public CompanyLookupDto? SelectedSearchCompany
    {
        get => _selectedSearchCompany;
        set
        {
            if (_selectedSearchCompany == value) return;
            _selectedSearchCompany = value;
            OnPropertyChanged();
            IsSearchSiteEnabled = value != null;
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
            if (value != null) _ = LoadSitesForSelectedCompanyAsync();
        }
    }

    private bool _isSearchSiteEnabled;
    public bool IsSearchSiteEnabled
    {
        get => _isSearchSiteEnabled;
        set { if (_isSearchSiteEnabled == value) return; _isSearchSiteEnabled = value; OnPropertyChanged(); }
    }

    private ObservableCollection<SiteLookupDto> _searchSiteSuggestions = new();
    public ObservableCollection<SiteLookupDto> SearchSiteSuggestions
    {
        get => _searchSiteSuggestions;
        set { _searchSiteSuggestions = value; OnPropertyChanged(); }
    }

    private SiteLookupDto? _selectedSearchSite;
    public SiteLookupDto? SelectedSearchSite
    {
        get => _selectedSearchSite;
        set
        {
            if (_selectedSearchSite == value) return;
            _selectedSearchSite = value;
            OnPropertyChanged();
        }
    }

    // --- Customer Search Selections ---
    private string? _customerSelectedCompanyLetter = "ALL";
    public string? CustomerSelectedCompanyLetter
    {
        get => _customerSelectedCompanyLetter;
        set
        {
            if (_customerSelectedCompanyLetter == value) return;
            _customerSelectedCompanyLetter = value;
            OnPropertyChanged();
            CustomerSelectedSearchCompany = null;
            CustomerSearchSiteSuggestions.Clear();
            CustomerSelectedSearchSite = null;
            ApplyCustomerCompanyLetterFilter();
        }
    }

    // --- Buyer Search Selections ---
    private string? _buyerSelectedCompanyLetter = "ALL";
    public string? BuyerSelectedCompanyLetter
    {
        get => _buyerSelectedCompanyLetter;
        set
        {
            if (_buyerSelectedCompanyLetter == value) return;
            _buyerSelectedCompanyLetter = value;
            OnPropertyChanged();
            BuyerSelectedSearchCompany = null;
            BuyerSearchSiteSuggestions.Clear();
            BuyerSelectedSearchSite = null;
            ApplyBuyerCompanyLetterFilter();
        }
    }

    // --- Customer Search Selections ---
    private ObservableCollection<CompanyLookupDto> _customerSearchCompanySuggestions = new();
    public ObservableCollection<CompanyLookupDto> CustomerSearchCompanySuggestions
    {
        get => _customerSearchCompanySuggestions;
        set { _customerSearchCompanySuggestions = value; OnPropertyChanged(); }
    }

    private CompanyLookupDto? _customerSelectedSearchCompany;
    public CompanyLookupDto? CustomerSelectedSearchCompany
    {
        get => _customerSelectedSearchCompany;
        set
        {
            if (_customerSelectedSearchCompany == value) return;
            _customerSelectedSearchCompany = value;
            OnPropertyChanged();
            IsCustomerSearchSiteEnabled = value != null;
            CustomerSearchSiteSuggestions.Clear();
            CustomerSelectedSearchSite = null;
            if (value != null) _ = LoadCustomerSearchSitesAsync();
        }
    }

    private bool _isCustomerSearchSiteEnabled;
    public bool IsCustomerSearchSiteEnabled
    {
        get => _isCustomerSearchSiteEnabled;
        set { if (_isCustomerSearchSiteEnabled == value) return; _isCustomerSearchSiteEnabled = value; OnPropertyChanged(); }
    }

    private ObservableCollection<SiteLookupDto> _customerSearchSiteSuggestions = new();
    public ObservableCollection<SiteLookupDto> CustomerSearchSiteSuggestions
    {
        get => _customerSearchSiteSuggestions;
        set { _customerSearchSiteSuggestions = value; OnPropertyChanged(); }
    }

    private string _customerSearchSiteIdText = string.Empty;
    public string CustomerSearchSiteIdText
    {
        get => _customerSearchSiteIdText;
        set { _customerSearchSiteIdText = value; OnPropertyChanged(); }
    }

    private SiteLookupDto? _customerSelectedSearchSite;
    public SiteLookupDto? CustomerSelectedSearchSite
    {
        get => _customerSelectedSearchSite;
        set
        {
            if (_customerSelectedSearchSite == value) return;
            _customerSelectedSearchSite = value;
            OnPropertyChanged();
            CustomerSearchSiteIdText = value?.SiteId.ToString() ?? string.Empty;
        }
    }

    // --- Buyer Search Selections ---
    private ObservableCollection<CompanyLookupDto> _buyerSearchCompanySuggestions = new();
    public ObservableCollection<CompanyLookupDto> BuyerSearchCompanySuggestions
    {
        get => _buyerSearchCompanySuggestions;
        set { _buyerSearchCompanySuggestions = value; OnPropertyChanged(); }
    }

    private CompanyLookupDto? _buyerSelectedSearchCompany;
    public CompanyLookupDto? BuyerSelectedSearchCompany
    {
        get => _buyerSelectedSearchCompany;
        set
        {
            if (_buyerSelectedSearchCompany == value) return;
            _buyerSelectedSearchCompany = value;
            OnPropertyChanged();
            IsBuyerSearchSiteEnabled = value != null;
            BuyerSearchSiteSuggestions.Clear();
            BuyerSelectedSearchSite = null;
            if (value != null) _ = LoadBuyerSearchSitesAsync();
        }
    }

    private bool _isBuyerSearchSiteEnabled;
    public bool IsBuyerSearchSiteEnabled
    {
        get => _isBuyerSearchSiteEnabled;
        set { if (_isBuyerSearchSiteEnabled == value) return; _isBuyerSearchSiteEnabled = value; OnPropertyChanged(); }
    }

    private ObservableCollection<SiteLookupDto> _buyerSearchSiteSuggestions = new();
    public ObservableCollection<SiteLookupDto> BuyerSearchSiteSuggestions
    {
        get => _buyerSearchSiteSuggestions;
        set { _buyerSearchSiteSuggestions = value; OnPropertyChanged(); }
    }

    private string _buyerSearchSiteIdText = string.Empty;
    public string BuyerSearchSiteIdText
    {
        get => _buyerSearchSiteIdText;
        set { _buyerSearchSiteIdText = value; OnPropertyChanged(); }
    }

    private SiteLookupDto? _buyerSelectedSearchSite;
    public SiteLookupDto? BuyerSelectedSearchSite
    {
        get => _buyerSelectedSearchSite;
        set
        {
            if (_buyerSelectedSearchSite == value) return;
            _buyerSelectedSearchSite = value;
            OnPropertyChanged();
            BuyerSearchSiteIdText = value?.SiteId.ToString() ?? string.Empty;
        }
    }

    // New customer: company + site selections
    private ObservableCollection<CompanyLookupDto> _newCompanySuggestions = new();

    public ObservableCollection<CompanyLookupDto> NewCompanySuggestions
    {
        get => _newCompanySuggestions;
        set
        {
            _newCompanySuggestions = value;
            OnPropertyChanged();
        }
    }

    private CompanyLookupDto? _selectedNewCompany;

    public CompanyLookupDto? SelectedNewCompany
    {
        get => _selectedNewCompany;
        set
        {
            if (_selectedNewCompany == value) return;

            _selectedNewCompany = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanUpdateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));
            OnPropertyChanged(nameof(CanUpdateBuyer));

            (UpdateCustomerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

            if (value != null)
            {
                // This string is what CreateCustomerAsync uses.
                NewCompanyName = value.CompanyName;

                SelectedNewSite = null;
                NewSiteSuggestions.Clear();
                OnPropertyChanged(nameof(CanCreateBuyer));
                (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

                // Load sites for the selected company
                _ = LoadNewSitesForSelectedCompanyAsync();
            }
            else
            {
                NewCompanyName = null;
                NewSiteSuggestions.Clear();
                SelectedNewSite = null;

                OnPropertyChanged(nameof(CanCreateCustomer));
                OnPropertyChanged(nameof(CanUpdateCustomer));
                OnPropertyChanged(nameof(CanCreateBuyer));
                OnPropertyChanged(nameof(CanUpdateBuyer));

                (UpdateCustomerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
                (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    private ObservableCollection<SiteLookupDto> _newSiteSuggestions = new();

    public ObservableCollection<SiteLookupDto> NewSiteSuggestions
    {
        get => _newSiteSuggestions;
        set
        {
            _newSiteSuggestions = value;
            OnPropertyChanged();
        }
    }

    private SiteLookupDto? _selectedNewSite;

    public SiteLookupDto? SelectedNewSite
    {
        get => _selectedNewSite;
        set
        {
            if (_selectedNewSite == value) return;

            _selectedNewSite = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanUpdateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));
            OnPropertyChanged(nameof(CanUpdateBuyer));
            (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

            UpdateNewLocationFromSelectedSite();
        }
    }

    private void UpdateNewLocationFromSelectedSite()
    {
        if (SelectedNewSite == null)
            return;

        NewAddressLine1 = SelectedNewSite.AddressLine1 ?? string.Empty;
        NewAddressLine2 = SelectedNewSite.AddressLine2 ?? string.Empty;
        NewSuburb = SelectedNewSite.Suburb ?? string.Empty;
        NewCity = SelectedNewSite.City ?? string.Empty;
        NewPostalCode = SelectedNewSite.PostalCode ?? string.Empty;

        if (SelectedNewSite.ProvinceId.HasValue)
            NewProvince = Provinces.FirstOrDefault(p => p.ProvinceId == SelectedNewSite.ProvinceId.Value);

        if (SelectedNewSite.CountryId.HasValue)
            NewCountry = Countries.FirstOrDefault(c => c.CountryId == SelectedNewSite.CountryId.Value);
    }

    // --------------------
    // Provinces (dropdown)
    // --------------------

    private ObservableCollection<ProvinceDto> _provinces = new();

    public ObservableCollection<ProvinceDto> Provinces
    {
        get => _provinces;
        set
        {
            _provinces = value;
            OnPropertyChanged();
        }
    }

    private ProvinceDto? _selectedProvince;

    public ProvinceDto? SelectedProvince
    {
        get => _selectedProvince;
        set
        {
            _selectedProvince = value;
            OnPropertyChanged();

            // If later you capture a "NewSiteProvinceId", set it here
            // NewSiteProvinceId = value?.ProvinceId;
        }
    }


    private ProvinceDto? _newProvince;

    public ProvinceDto? NewProvince
    {
        get => _newProvince;
        set
        {
            _newProvince = value;
            OnPropertyChanged();
        }
    }

    private ProvinceDto? _customerSearchProvince;
    public ProvinceDto? CustomerSearchProvince
    {
        get => _customerSearchProvince;
        set { _customerSearchProvince = value; OnPropertyChanged(); }
    }

    private ProvinceDto? _buyerSearchProvince;
    public ProvinceDto? BuyerSearchProvince
    {
        get => _buyerSearchProvince;
        set { _buyerSearchProvince = value; OnPropertyChanged(); }
    }

    // 🔹 NEW: search-only provinces (includes "ALL")
    private ObservableCollection<ProvinceDto> _searchProvinces = new();

    public ObservableCollection<ProvinceDto> SearchProvinces
    {
        get => _searchProvinces;
        set
        {
            _searchProvinces = value;
            OnPropertyChanged();
        }
    }

    // Countries (dropdown) – for now just South Africa, but shaped for future API
    private ObservableCollection<CountryDto> _countries = new();

    public ObservableCollection<CountryDto> Countries
    {
        get => _countries;
        set
        {
            _countries = value;
            OnPropertyChanged();
        }
    }

    private CountryDto? _selectedCountry;

    public CountryDto? SelectedCountry
    {
        get => _selectedCountry;
        set
        {
            _selectedCountry = value;
            OnPropertyChanged();
        }
    }

    private CountryDto? _newCountry;

    public CountryDto? NewCountry
    {
        get => _newCountry;
        set
        {
            _newCountry = value;
            OnPropertyChanged();
        }
    }

    private CountryDto? _customerSearchCountry;
    public CountryDto? CustomerSearchCountry
    {
        get => _customerSearchCountry;
        set { _customerSearchCountry = value; OnPropertyChanged(); }
    }

    private CountryDto? _buyerSearchCountry;
    public CountryDto? BuyerSearchCountry
    {
        get => _buyerSearchCountry;
        set { _buyerSearchCountry = value; OnPropertyChanged(); }
    }

    // 🔹 NEW: search-only countries (includes "ALL")
    private ObservableCollection<CountryDto> _searchCountries = new();

    public ObservableCollection<CountryDto> SearchCountries
    {
        get => _searchCountries;
        set
        {
            _searchCountries = value;
            OnPropertyChanged();
        }
    }

    // Read-only site address summary for selected customer (from Site)
    private string _customerSiteAddressSummary = string.Empty;
    public string CustomerSiteAddressSummary
    {
        get => _customerSiteAddressSummary;
        private set
        {
            if (_customerSiteAddressSummary == value) return;
            _customerSiteAddressSummary = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _buyerSiteAddressSummary = string.Empty;
    public string BuyerSiteAddressSummary
    {
        get => _buyerSiteAddressSummary;
        private set
        {
            if (_buyerSiteAddressSummary == value) return;
            _buyerSiteAddressSummary = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public void InitializeCountries()
    {
        // Avoid re-initialising if already done
        if (Countries.Count > 0 && SearchCountries.Count > 0)
            return;

        Countries.Clear();
        SearchCountries.Clear();

        // Real country row
        var southAfrica = new CountryDto
        {
            CountryId = 1,
            CountryName = "South Africa",
            CountryCode = "ZA"
        };

        Countries.Add(southAfrica);

        // 🔹 Search list: add real country, then insert "ALL" at the top
        SearchCountries.Add(southAfrica);

        var allCountry = new CountryDto
        {
            CountryId = 0,
            CountryName = "ALL",
            CountryCode = "ALL"
        };

        // ALL at index 0
        SearchCountries.Insert(0, allCountry);

        // 🔹 Defaults
        // Create/Edit → South Africa
        _selectedCountry = southAfrica;
        NewCountry = southAfrica;
        OnPropertyChanged(nameof(SelectedCountry));
        OnPropertyChanged(nameof(NewCountry));

        // Search → ALL (meaning "no country filter")
        CustomerSearchCountry = allCountry;
        BuyerSearchCountry = allCountry;
        OnPropertyChanged(nameof(CustomerSearchCountry));
        OnPropertyChanged(nameof(BuyerSearchCountry));
    }

    public async Task LoadProvincesAsync()
    {
        var items = await _provinceService.GetAllAsync();

        Provinces.Clear();
        SearchProvinces.Clear();

        if (items != null)
        {
            foreach (var p in items)
            {
                Provinces.Add(p);
                SearchProvinces.Add(p);
            }
        }

        // 🔹 Add "ALL" to the top of the SEARCH list only
        var allProvince = new ProvinceDto
        {
            ProvinceId = 0,
            ProvinceName = "ALL",
            ProvinceCode = "ALL"
        };

        SearchProvinces.Insert(0, allProvince);

        // 🔹 Default create/edit → Gauteng
        var gauteng = Provinces.FirstOrDefault(p => p.ProvinceName == "Gauteng");
        if (gauteng is not null)
        {
            _selectedProvince = gauteng;
            NewProvince = gauteng;
            OnPropertyChanged(nameof(SelectedProvince));
            OnPropertyChanged(nameof(NewProvince));
        }

        // 🔹 Default search → ALL (meaning "no province filter")
        CustomerSearchProvince = allProvince;
        BuyerSearchProvince = allProvince;
        OnPropertyChanged(nameof(CustomerSearchProvince));
        OnPropertyChanged(nameof(BuyerSearchProvince));
    }
}
