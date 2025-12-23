using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using MetalLink.Shared.Provinces;
using MetalLink.Shared.Locations;
using MetalLink.Shared.Customers;
using Avalonia.Threading;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Lazy-created services (we already have _apiClient in the core partial)
    private CompanyService?  _companyService;
    private SiteService?     _siteService;
    private ProvinceService? _provinceService;

    private CompanyService CompanyService =>
        _companyService ??= new CompanyService(_apiClient);

    private SiteService SiteService =>
        _siteService ??= new SiteService(_apiClient);

    private ProvinceService ProvinceService =>
        _provinceService ??= new ProvinceService(_apiClient);

    // =====================================================
    // SHARED COMPANY MASTER LIST + LETTER FILTERS
    // =====================================================

    private readonly ObservableCollection<CompanyLookupDto> _allCompanies        = new();
    private readonly ObservableCollection<string>           _companyLetterFilters = new();

    private bool _companyLettersLoaded;
    private bool _companyLettersLoading;
    private bool _syncingCompanyLetter;
    private bool _suppressLetterApply;

    /// <summary>
    /// Letters used by the "ALL / A / B / C / …" filter ComboBoxes.
    /// Populated lazily the first time the view binds to it.
    /// </summary>
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

    // ---------------- Search section letter ----------------

    private string? _selectedCompanyLetter;
    public string? SelectedCompanyLetter
    {
        get => _selectedCompanyLetter;
        set
        {
            if (_selectedCompanyLetter == value) return;
            _selectedCompanyLetter = value;
            OnPropertyChanged();

            // If we're in the middle of syncing because company selection changed,
            // don't rebuild the combo's ItemsSource immediately.
            if (_suppressLetterApply)
                return;

            // Defer rebuilding the company suggestions until after the selection event finishes.
            PostUI(() =>
            {
                ApplyCompanyLetterFilter();

                // Optional: If the user manually changed the letter (not caused by selecting a company)
                // clear company selection AFTER the filter is applied.
                if (!_syncingCompanyLetter)
                    SelectedSearchCompany = null;
            });
        }
    }


    // --------------- Create section letter -----------------

    private string? _selectedNewCompanyLetter;
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

    /// <summary>
    /// Convenience alias for the create section XAML.
    /// </summary>
    public ObservableCollection<string> NewCompanyLetterFilters => CompanyLetterFilters;

    /// <summary>
    /// Load all companies from the API, build the letter list and
    /// apply the initial "ALL" filters for search and create areas.
    /// </summary>
    private async Task LoadCompaniesAndLettersAsync()
    {
        var items = await CompanyService.LookupCompaniesAsync(string.Empty);

        _allCompanies.Clear();
        if (items != null)
        {
            foreach (var c in items.OrderBy(c => c.CompanyName))
                _allCompanies.Add(c);
        }

        // Build distinct A–Z list from company names
        _companyLetterFilters.Clear();
        _companyLetterFilters.Add("ALL");

        var letters = _allCompanies
            .Select(c => c.CompanyName?.FirstOrDefault() ?? '\0')
            .Where(ch => ch != '\0')
            .Select(ch => char.ToUpperInvariant(ch))
            .Distinct()
            .OrderBy(ch => ch);

        foreach (var ch in letters)
            _companyLetterFilters.Add(ch.ToString());

        _companyLettersLoaded  = true;
        _companyLettersLoading = false;

        // Default to ALL on first load (search area)
        if (SelectedCompanyLetter == null)
            SelectedCompanyLetter = "ALL";
        else
            ApplyCompanyLetterFilter();

        // Default to ALL for the create-customer area too
        if (SelectedNewCompanyLetter == null)
            SelectedNewCompanyLetter = "ALL";
        else
            ApplyNewCompanyLetterFilter();
    }

    // =====================================================
    // SEARCH CUSTOMERS – COMPANY + SITE
    // =====================================================

    /// <summary>
    /// Companies matching the chosen letter (or ALL).
    /// Bound to the search "Company" ComboBox.
    /// </summary>
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

            if (value != null)
            {
                var first = value.CompanyName?.FirstOrDefault();
                var letter = first.HasValue ? char.ToUpperInvariant(first.Value).ToString() : "ALL";

                // Defer the letter change so we don't mutate ItemsSource mid-selection
                PostUI(() =>
                {
                    _syncingCompanyLetter = true;
                    _suppressLetterApply = true;

                    SelectedCompanyLetter = CompanyLetterFilters.Contains(letter) ? letter : "ALL";

                    _suppressLetterApply = false;
                    _syncingCompanyLetter = false;

                    // Now rebuild suggestions and keep the selected company pinned in the list
                    ApplyCompanyLetterFilter();

                    // Ensure the selected company still exists in the filtered suggestions
                    // (and doesn't get cleared by Contains() checks)
                    if (!SearchCompanySuggestions.Contains(value))
                    {
                        // if your filter excluded it somehow, force-add it at top
                        SearchCompanySuggestions.Insert(0, value);
                    }
                });

                // keep your existing behaviour if needed
                SearchCompanyNameText = value.CompanyName;
                IsSearchSiteEnabled = true;
                _ = LoadSitesForSelectedCompanyAsync();
            }
            else
            {
                SearchCompanyNameText = string.Empty;
                IsSearchSiteEnabled = false;
                SearchSiteIdText = string.Empty;

                SearchSiteSuggestions.Clear();
                SelectedSearchSite = null;
            }
        }
    }


    /// <summary>
    /// Rebuild SearchCompanySuggestions based on SelectedCompanyLetter.
    /// </summary>
    private void ApplyCompanyLetterFilter()
    {
        if (!_companyLettersLoaded)
            return;

        var letter = SelectedCompanyLetter;

        SearchCompanySuggestions.Clear();

        var query = _allCompanies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(letter) &&
            !string.Equals(letter, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c =>
                !string.IsNullOrWhiteSpace(c.CompanyName) &&
                char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }

        foreach (var c in query)
            SearchCompanySuggestions.Add(c);

        // If current selection no longer fits filter, clear it
        if (SelectedSearchCompany != null &&
            !SearchCompanySuggestions.Contains(SelectedSearchCompany))
        {
            SelectedSearchCompany = null;
        }
    }

    // ----- Customer -----
    private long? _pendingSelectSiteId;

    private void OnEditCustomer(Shared.Customers.CustomerDto? customer)
    {
        if (customer == null)
            return;

        EditingCustomerId = customer.CustomerId;
        IsEditMode        = true;

        // -----------------------
        // Names (already fixed on API, but keep safe)
        // -----------------------
        NewFirstName = customer.FirstName ?? string.Empty;
        NewLastName  = customer.LastName  ?? string.Empty;

        // -----------------------
        // Basic contact / address
        // -----------------------
        NewIdNumber      = customer.IdNumber      ?? string.Empty;
        NewAccountNumber = customer.AccountNumber;
        NewPriceCode     = customer.PriceCode     ?? string.Empty;
        NewTaxable       = customer.Taxable;
        NewPhoneNumber   = customer.PhoneNumber   ?? string.Empty;
        NewMobileNumber  = customer.MobileNumber  ?? string.Empty;
        NewEmail         = customer.Email         ?? string.Empty;
        NewAddressLine1  = customer.AddressLine1  ?? string.Empty;
        NewAddressLine2  = customer.AddressLine2  ?? string.Empty;
        NewSuburb        = customer.Suburb        ?? string.Empty;
        NewCity          = customer.City          ?? string.Empty;
        NewPostalCode    = customer.PostalCode    ?? string.Empty;

        // -----------------------
        // Company / site mode
        // -----------------------
        NewIsCompany = customer.IsCompany
                || customer.CompanyId.HasValue
                || customer.SiteId.HasValue;   // <-- use actual flag

        // Try to locate the company in the cached lookup list.
        // First by ID, then (if needed) by name.
        CompanyLookupDto? company = null;

        if (customer.CompanyId.HasValue)
        {
            company = _allCompanies
                .FirstOrDefault(c => c.CompanyId == customer.CompanyId.Value);
        }

        if (company == null && !string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            company = _allCompanies
                .FirstOrDefault(c =>
                    string.Equals(c.CompanyName,
                                customer.CompanyName,
                                StringComparison.OrdinalIgnoreCase));
        }

        if (company != null)
        {
            var letter   = char.ToUpperInvariant(company.CompanyName?.FirstOrDefault() ?? 'A');
            var letterStr = letter.ToString();

            if (!CompanyLetterFilters.Contains(letterStr))
                letterStr = "ALL";

            // This will rebuild NewCompanySuggestions via ApplyNewCompanyLetterFilter
            SelectedNewCompanyLetter = letterStr;

            // Set the actual selection used by the Create/Edit combobox
            SelectedNewCompany = company;
        }
        else
        {
            SelectedNewCompanyLetter = "ALL";
            SelectedNewCompany       = null;
        }

        // Load sites for the company and select the correct one
        _pendingSelectSiteId = customer.SiteId;
        OnPropertyChanged(nameof(CanCreateCustomer));
        OnPropertyChanged(nameof(CanUpdateCustomer));
        (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        _ = LoadNewSitesAndSelectAsync(customer.SiteId);
    }

    private async Task OnDeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null)
            return;

        // TODO: show your confirm dialog here

        await _customerService.SoftDeleteCustomerAsync(customer.CustomerId);

        CustomerSearchResults.Remove(customer);
        StatusMessage = $"Customer {customer.FirstName} {customer.LastName} was deleted (soft delete).";
    }

    private async Task LoadNewSitesAndSelectAsync(long? siteId)
    {
        NewSiteSuggestions.Clear();

        if (SelectedNewCompany == null)
        {
            SelectedNewSite = null;
            return;
        }

        var sites = await SiteService.LookupSitesForCompanyAsync(SelectedNewCompany.CompanyId, string.Empty);

        if (sites != null)
        {
            foreach (var s in sites.OrderBy(s => s.SiteName))
                NewSiteSuggestions.Add(s);
        }

        if (siteId.HasValue)
        {
            var match = NewSiteSuggestions.FirstOrDefault(s => s.SiteId == siteId.Value);
            SelectedNewSite = match;
        }
        else
        {
            SelectedNewSite = null;
        }
    }

    private async Task ClearNewCustomerFormAsync()
    {
        EditingCustomerId = null;
        IsEditMode = false;

        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewIdNumber = string.Empty;

        try
        {
            // assign the next available account number
            NewAccountNumber = await _customerService.GetNextAccountNumberAsync();
        }
        catch (Exception ex)
        {
            // Don't crash the app. Log + fall back to null/empty display.
            Console.WriteLine($"GetNextAccountNumberAsync failed: {ex}");
            NewAccountNumber = null;
        }

        NewPriceCode = string.Empty;
        NewPhoneNumber = string.Empty;
        NewMobileNumber = string.Empty;
        NewEmail = string.Empty;
        NewAddressLine1 = string.Empty;
        NewAddressLine2 = string.Empty;
        NewSuburb = string.Empty;
        NewCity = string.Empty;
        NewPostalCode = string.Empty;

        NewIsCompany = false;
        SelectedNewCompanyLetter = "ALL";
        SelectedNewCompany = null;
        NewSiteSuggestions.Clear();
        SelectedNewSite = null;
    }

    private async Task LoadNextAccountNumberAsync()
    {
        try
        {
            // You’ll implement this method on your Desktop CustomerService
            var next = await _customerService.GetNextAccountNumberAsync();
            NewAccountNumber = next;
            OnPropertyChanged(nameof(NewAccountNumberDisplay));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
        catch
        {
            // optional: keep it null or set a safe default
            NewAccountNumber = null;
            OnPropertyChanged(nameof(NewAccountNumberDisplay));
        }
    }

    private string _searchAccountNumberText = string.Empty;
    public string SearchAccountNumberText
    {
        get => _searchAccountNumberText;
        set
        {
            _searchAccountNumberText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private long? ParseAccountNumberOrNull(string text)
    {
        var t = (text ?? "").Trim();

        if (string.IsNullOrEmpty(t))
            return null;

        // treat "0", "00", "0000" etc as "no filter"
        if (t.All(c => c == '0'))
            return null;

        return long.TryParse(t, out var v) ? v : null;
    }

    private async Task OnUpdateCustomerAsync()
    {
        if (!IsEditMode || EditingCustomerId == null)
            return;

        // Basic validation: company + site required when IsCompany
        if (NewIsCompany && (SelectedNewCompany == null || SelectedNewSite == null))
        {
            StatusMessage = "Select a company and site before updating.";
            return;
        }

        var dto = new CustomerDto
        {
            CustomerId    = EditingCustomerId.Value,
            FirstName     = NewFirstName,
            LastName      = NewLastName,
            IdNumber      = NewIdNumber,
            AccountNumber = NewAccountNumber,
            PriceCode     = NewPriceCode,
            PhoneNumber   = NewPhoneNumber,
            MobileNumber  = NewMobileNumber,
            Email         = NewEmail,
            Taxable       = NewTaxable,
            AddressLine1  = NewAddressLine1,
            AddressLine2  = NewAddressLine2,
            Suburb        = NewSuburb,
            City          = NewCity,
            PostalCode    = NewPostalCode,
            IsCompany     = NewIsCompany,

            // We KNOW these are non-null if NewIsCompany is true
            // because of the validation above.
            CompanyId = SelectedNewCompany != null
                ? SelectedNewCompany.CompanyId
                : null,   // will be null for non-company customers

            SiteId = SelectedNewSite != null
                ? SelectedNewSite.SiteId
                : null
        };

        await _customerService.UpdateCustomerAsync(dto);
        FoundCustomer = await _customerService.GetCustomerByIdAsync(dto.CustomerId);

        // Pull fresh copy from API (includes SiteName + AddressLine2 etc)
        var refreshed = await _customerService.GetCustomerByIdAsync(dto.CustomerId);

        // Fallback if API returns null for any reason
        refreshed ??= dto;

        var existing = CustomerSearchResults.FirstOrDefault(c => c.CustomerId == dto.CustomerId);
        if (existing != null)
        {
            var index = CustomerSearchResults.IndexOf(existing);
            if (index >= 0)
                CustomerSearchResults[index] = refreshed; // replace item (forces UI refresh)
        }
        else
        {
            CustomerSearchResults.Add(refreshed);
        }

        // update details panel immediately
        FoundCustomer = refreshed;


        await ClearNewCustomerFormAsync();
        _newAccountNumber = await _customerService.GetNextAccountNumberAsync();
        OnPropertyChanged(nameof(NewAccountNumber));
        OnPropertyChanged(nameof(CanCreateCustomer));
    }

    private void OnLogTicket(CustomerDto? customer)
    {
        if (customer == null)
            return;

        // Pre-fill the Ticket screen with this customer's ID (optional)
        TicketCustomerIdText = customer.CustomerId.ToString("D8");

        // Switch to the Tickets section – this uses the same enum
        // you already use in ShowTicketsCommand.
        CurrentSection = EnumMainSection.Tickets;

        StatusMessage = $"Logging ticket for customer {customer.FirstName} {customer.LastName} - ({customer.CustomerId:D8}).";
    }

    // ----- Search Customers: Site -----

    private bool _isSearchSiteEnabled;
    public bool IsSearchSiteEnabled
    {
        get => _isSearchSiteEnabled;
        set { _isSearchSiteEnabled = value; OnPropertyChanged(); }
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

            if (value != null)
            {
                SearchSiteIdText = value.SiteId.ToString();
            }
            else
            {
                SearchSiteIdText = string.Empty;
            }
        }
    }

    private async Task LoadSitesForSelectedCompanyAsync()
    {
        if (SelectedSearchCompany == null)
            return;

        var companyId = SelectedSearchCompany.CompanyId;
        var items     = await SiteService.LookupSitesForCompanyAsync(companyId, string.Empty);

        SearchSiteSuggestions.Clear();

        if (items != null)
        {
            foreach (var s in items.OrderBy(s => s.SiteName))
                SearchSiteSuggestions.Add(s);
        }

        if (SelectedSearchSite != null &&
            !SearchSiteSuggestions.Contains(SelectedSearchSite))
        {
            SelectedSearchSite = null;
        }
    }

    // =====================================================
    // CREATE CUSTOMER – COMPANY + SITE (LETTER FILTER)
    // =====================================================

    private ObservableCollection<CompanyLookupDto> _newCompanySuggestions = new();
    public ObservableCollection<CompanyLookupDto> NewCompanySuggestions
    {
        get => _newCompanySuggestions;
        set { _newCompanySuggestions = value; OnPropertyChanged(); }
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

            if (value != null)
            {
                // This string is what CreateCustomerAsync uses.
                NewCompanyName = value.CompanyName;

                SelectedNewSite = null;
                NewSiteSuggestions.Clear();

                // Load sites for the selected company
                _ = LoadNewSitesForSelectedCompanyAsync();
            }
            else
            {
                NewCompanyName = null;
                NewSiteSuggestions.Clear();
                SelectedNewSite = null;
            }
        }
    }

    /// <summary>
    /// Rebuilds NewCompanySuggestions based on SelectedNewCompanyLetter.
    /// </summary>
    private void ApplyNewCompanyLetterFilter()
    {
        if (!_companyLettersLoaded)
            return;

        var letter = SelectedNewCompanyLetter;

        NewCompanySuggestions.Clear();

        var query = _allCompanies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(letter) &&
            !string.Equals(letter, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c =>
                !string.IsNullOrWhiteSpace(c.CompanyName) &&
                char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }

        foreach (var c in query)
            NewCompanySuggestions.Add(c);

        // Clear selection if it no longer matches the filter.
        if (SelectedNewCompany != null &&
            !NewCompanySuggestions.Contains(SelectedNewCompany))
        {
            SelectedNewCompany = null;
        }
    }

    private ObservableCollection<SiteLookupDto> _newSiteSuggestions = new();
    public ObservableCollection<SiteLookupDto> NewSiteSuggestions
    {
        get => _newSiteSuggestions;
        set { _newSiteSuggestions = value; OnPropertyChanged(); }
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

            UpdateNewLocationFromSelectedSite();
        }
    }

    private void UpdateNewLocationFromSelectedSite()
    {
        // Nothing selected – nothing to sync
        if (SelectedNewSite == null)
            return;

        // 🔹 Province: match by Id into the Provinces collection
        if (SelectedNewSite.ProvinceId.HasValue && Provinces is { Count: > 0 })
        {
            var province = Provinces.FirstOrDefault(
                p => p.ProvinceId == SelectedNewSite.ProvinceId.Value);

            if (province != null)
            {
                NewProvince = province;
            }
        }

        // 🔹 Country: match by Id into the Countries collection
        if (SelectedNewSite.CountryId.HasValue && Countries is { Count: > 0 })
        {
            var country = Countries.FirstOrDefault(
                c => c.CountryId == SelectedNewSite.CountryId.Value);

            if (country != null)
            {
                NewCountry = country;
            }
        }
    }

    // Which customer (if any) are we editing?
    private long? _editingCustomerId;
    public long? EditingCustomerId
    {
        get => _editingCustomerId;
        set
        {
            if (_editingCustomerId == value) return;
            _editingCustomerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateCustomer));

            // ✅ IMPORTANT: refresh command CanExecute
            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode == value) return;
            _isEditMode = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanUpdateCustomer));
            OnPropertyChanged(nameof(IsCreateMode)); // you already expose IsCreateMode

            // ✅ IMPORTANT: refresh command CanExecute
            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

// Convenience flag for binding Create button
public bool IsCreateMode => !IsEditMode;


    /// <summary>
    /// Loads all sites for the SelectedNewCompany into NewSiteSuggestions.
    /// </summary>
    private async Task LoadNewSitesForSelectedCompanyAsync()
    {
        if (SelectedNewCompany == null)
        {
            NewSiteSuggestions.Clear();
            SelectedNewSite = null;
            return;
        }

        var companyId = SelectedNewCompany.CompanyId;
        var results   = await SiteService.LookupSitesForCompanyAsync(companyId, string.Empty);

        NewSiteSuggestions.Clear();

        if (results != null)
        {
            foreach (var s in results.OrderBy(s => s.SiteName))
                NewSiteSuggestions.Add(s);
        }

        if (SelectedNewSite != null &&
            !NewSiteSuggestions.Contains(SelectedNewSite))
        {
            SelectedNewSite = null;
        }

        if (_pendingSelectSiteId.HasValue)
        {
            var match = NewSiteSuggestions.FirstOrDefault(s => s.SiteId == _pendingSelectSiteId.Value);
            SelectedNewSite = match;
            _pendingSelectSiteId = null;
        }
    }

    // --------------------
    // Provinces (dropdown)
    // --------------------

    private ObservableCollection<ProvinceDto> _provinces = new();
    public ObservableCollection<ProvinceDto> Provinces
    {
        get => _provinces;
        set { _provinces = value; OnPropertyChanged(); }
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
        set { _newProvince = value; OnPropertyChanged(); }
    }

    private ProvinceDto? _searchProvince;
    public ProvinceDto? SearchProvince
    {
        get => _searchProvince;
        set { _searchProvince = value; OnPropertyChanged(); }
    }

    // 🔹 NEW: search-only provinces (includes "ALL")
    private ObservableCollection<ProvinceDto> _searchProvinces = new();
    public ObservableCollection<ProvinceDto> SearchProvinces
    {
        get => _searchProvinces;
        set { _searchProvinces = value; OnPropertyChanged(); }
    }

    // Countries (dropdown) – for now just South Africa, but shaped for future API
    private ObservableCollection<CountryDto> _countries = new();

    public ObservableCollection<CountryDto> Countries
    {
        get => _countries;
        set { _countries = value; OnPropertyChanged(); }
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
        set { _newCountry = value; OnPropertyChanged(); }
    }

    private CountryDto? _searchCountry;
    public CountryDto? SearchCountry
    {
        get => _searchCountry;
        set { _searchCountry = value; OnPropertyChanged(); }
    }

    // 🔹 NEW: search-only countries (includes "ALL")
    private ObservableCollection<CountryDto> _searchCountries = new();
    public ObservableCollection<CountryDto> SearchCountries
    {
        get => _searchCountries;
        set { _searchCountries = value; OnPropertyChanged(); }
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
            Name      = "South Africa",
            Code      = "ZA"
        };

        Countries.Add(southAfrica);

        // 🔹 Search list: add real country, then insert "ALL" at the top
        SearchCountries.Add(southAfrica);

        var allCountry = new CountryDto
        {
            CountryId = 0,
            Name      = "ALL",
            Code      = "ALL"
        };

        // ALL at index 0
        SearchCountries.Insert(0, allCountry);

        // 🔹 Defaults
        // Create/Edit → South Africa
        _selectedCountry = southAfrica;
        NewCountry       = southAfrica;
        OnPropertyChanged(nameof(SelectedCountry));
        OnPropertyChanged(nameof(NewCountry));

        // Search → ALL (meaning "no country filter")
        SearchCountry = allCountry;
        OnPropertyChanged(nameof(SearchCountry));
    }

    public async Task LoadProvincesAsync()
    {
        var items = await ProvinceService.GetAllAsync();

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
            ProvinceId   = 0,
            ProvinceName = "ALL",
            ProvinceCode = "ALL"
        };

        SearchProvinces.Insert(0, allProvince);

        // 🔹 Default create/edit → Gauteng
        var gauteng = Provinces.FirstOrDefault(p => p.ProvinceName == "Gauteng");
        if (gauteng is not null)
        {
            _selectedProvince = gauteng;
            NewProvince       = gauteng;
            OnPropertyChanged(nameof(SelectedProvince));
            OnPropertyChanged(nameof(NewProvince));
        }

        // 🔹 Default search → ALL (meaning "no province filter")
        SearchProvince = allProvince;
        OnPropertyChanged(nameof(SearchProvince));
    }

    private void PostUI(Action action)
    {
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
    }
}
