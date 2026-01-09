using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using MetalLink.Shared.Customers;
using MetalLink.Desktop.Services;
using System.Collections.Generic;
using System.Threading;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Lazy-created services (we already have _apiClient in the core partial)
    // ----- Customer -----
    private bool _suppressLetterApply;

    private void OnEditCustomer(Shared.Customers.CustomerDto? customer)
    {
        if (customer == null)
            return;

        EditingCustomerId = customer.CustomerId;
        IsEditMode = true;

        // -----------------------
        // Names (already fixed on API, but keep safe)
        // -----------------------
        NewFirstName = customer.FirstName ?? string.Empty;
        NewLastName = customer.LastName ?? string.Empty;

        // -----------------------
        // Basic contact / address
        // -----------------------
        NewIdNumber = customer.IdNumber ?? string.Empty;
        NewAccountNumber = customer.AccountNumber;
        NewPriceCode = customer.PriceCode ?? string.Empty;
        NewTaxable = customer.Taxable;
        NewPhoneNumber = customer.PhoneNumber ?? string.Empty;
        NewMobileNumber = customer.MobileNumber ?? string.Empty;
        NewEmail = customer.Email ?? string.Empty;
        NewAddressLine1 = customer.AddressLine1 ?? string.Empty;
        NewAddressLine2 = customer.AddressLine2 ?? string.Empty;
        NewSuburb = customer.Suburb ?? string.Empty;
        NewCity = customer.City ?? string.Empty;
        NewPostalCode = customer.PostalCode ?? string.Empty;

        // -----------------------
        // Company / site mode
        // -----------------------
        NewIsCompany = customer.IsCompany
                       || customer.CompanyId.HasValue
                       || customer.SiteId.HasValue; // <-- use actual flag

        // Try to locate the company in the cached lookup list.
        // First by ID, then (if needed) by name.
        CompanyLookupDto? company = null;

        SyncPriceCodeDropdownFromNewPriceCode();

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
            var letter = char.ToUpperInvariant(company.CompanyName?.FirstOrDefault() ?? 'A');
            var letterStr = letter.ToString();

            if (!CompanyLetterFilters.Contains(letterStr))
                letterStr = "ALL";

            // This will rebuild NewCompanySuggestions via ApplyNewCompanyLetterFilter
            SelectedCompanyLetter = letterStr;

            // Set the actual selection used by the Create/Edit combobox
            SelectedNewCompany = company;
        }
        else
        {
            SelectedCompanyLetter = "ALL";
            SelectedNewCompany = null;
        }

        // Load sites for the company and select the correct one
        _pendingSelectSiteId = customer.SiteId;
        OnPropertyChanged(nameof(CanCreateCustomer));
        OnPropertyChanged(nameof(CanUpdateCustomer));
        (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        _ = LoadNewSitesAndSelectAsync(customer.SiteId);
    }

    private void ClearCustomerSearch()
    {
        SearchCustomerIdText = string.Empty;
        SearchSiteIdText = string.Empty;
        SearchFirstNameText = string.Empty;
        SearchLastNameText = string.Empty;
        SearchCompanyNameText = string.Empty;
        SearchIdNumberText = string.Empty;
        SearchAddressLine1Text = string.Empty;
        SearchAddressLine2Text = string.Empty;
        SearchSuburbText = string.Empty;
        SearchCityText = string.Empty;
        SearchPostalCodeText = string.Empty;
        SearchPhoneNumberText = string.Empty;
        SearchMobileNumberText = string.Empty;
        SearchEmailText = string.Empty;

        // ✅ IMPORTANT: reset dropdowns
        SearchPriceCode = null;
        SearchTaxable = true;

        // Optional: reset site/company dropdowns if used
        SelectedSearchCompany = null;
        SelectedSearchSite = null;

        // Optional: reload all customers
        //_ = SearchCustomersAsync();
    }

    private async Task OnDeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null)
            return;

        if (IsBusy)
            return;

        var ok = await ConfirmAsync($"Are you sure you want to delete - {customer.FirstName} {customer.LastName} ?");
        if (!ok)
            return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Deleting customer...";

            await _customerService.SoftDeleteCustomerAsync(customer.CustomerId);

            CustomerSearchResults.Remove(customer);

            if (FoundCustomer?.CustomerId == customer.CustomerId)
            {
                FoundCustomer = null;
            }

            // If we were editing this customer, reset the form
            if (EditingCustomerId == customer.CustomerId)
            {
                await ClearNewCustomerFormAsync();
            }

            StatusMessage = $"[STATUS] Customer {customer.FirstName} {customer.LastName} deleted (soft).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Delete customer failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
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

        SelectedPriceCodeChar = null;
        NewPhoneNumber = string.Empty;
        NewMobileNumber = string.Empty;
        NewEmail = string.Empty;
        NewAddressLine1 = string.Empty;
        NewAddressLine2 = string.Empty;
        NewSuburb = string.Empty;
        NewCity = string.Empty;
        NewPostalCode = string.Empty;

        NewIsCompany = false;
        SelectedCompanyLetter = "ALL";
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
            CustomerId = EditingCustomerId.Value,
            FirstName = NewFirstName,
            LastName = NewLastName,
            IdNumber = NewIdNumber,
            AccountNumber = NewAccountNumber,
            PriceCode = SelectedPriceCodeChar?.Code.Trim(),
            PhoneNumber = NewPhoneNumber,
            MobileNumber = NewMobileNumber,
            Email = NewEmail,
            Taxable = NewTaxable,
            AddressLine1 = NewAddressLine1,
            AddressLine2 = NewAddressLine2,
            Suburb = NewSuburb,
            City = NewCity,
            PostalCode = NewPostalCode,
            IsCompany = NewIsCompany,

            // We KNOW these are non-null if NewIsCompany is true
            // because of the validation above.
            CompanyId = SelectedNewCompany != null
                ? SelectedNewCompany.CompanyId
                : null, // will be null for non-company customers

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

        StatusMessage =
            $"Logging ticket for customer {customer.FirstName} {customer.LastName} - ({customer.CustomerId:D8}).";
    }

    // ----- Search Customers: Site -----

    private bool _isSearchSiteEnabled;

    public bool IsSearchSiteEnabled
    {
        get => _isSearchSiteEnabled;
        set
        {
            if (_isSearchSiteEnabled == value) return;
            _isSearchSiteEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _isNewSiteEnabled;

    public bool IsNewSiteEnabled
    {
        get => _isNewSiteEnabled;
        set
        {
            _isNewSiteEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewSiteEnabled));
        }
    }


    private ObservableCollection<SiteLookupDto> _searchSiteSuggestions = new();

    public ObservableCollection<SiteLookupDto> SearchSiteSuggestions
    {
        get => _searchSiteSuggestions;
        set
        {
            _searchSiteSuggestions = value;
            OnPropertyChanged();
        }
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
        {
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
            IsSearchSiteEnabled = false;
            return;
        }

        try
        {
            IsSearchSiteEnabled = true;

            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;

            var sites = await SiteService.LookupSitesForCompanyAsync(
                SelectedSearchCompany.CompanyId,
                term: "",
                CancellationToken.None);

            if (sites != null)
            {
                foreach (var s in sites.OrderBy(x => x.SiteName))
                    SearchSiteSuggestions.Add(s);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Load sites failed: {ex.Message}";
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
            IsSearchSiteEnabled = false;
        }
    }

    // =====================================================
    // CREATE CUSTOMER – COMPANY + SITE (LETTER FILTER)
    // =====================================================

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

    private void ApplyNewCompanyLetterFilter()
    {
        if (!_companyLettersLoaded)
            return;

        var selectedId = SelectedNewCompany?.CompanyId;

        var letter = (SelectedNewCompanyLetter ?? "ALL").Trim();

        NewCompanySuggestions.Clear();

        IEnumerable<CompanyLookupDto> query = _allCompanies.AsEnumerable();

        if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c =>
                !string.IsNullOrWhiteSpace(c.CompanyName) &&
                char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }

        foreach (var c in query.OrderBy(c => c.CompanyName))
            NewCompanySuggestions.Add(c);

        // ✅ preserve selection by ID
        if (selectedId.HasValue)
            SelectedNewCompany = NewCompanySuggestions.FirstOrDefault(x => x.CompanyId == selectedId.Value);
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
            var province = Provinces.FirstOrDefault(p => p.ProvinceId == SelectedNewSite.ProvinceId.Value);

            if (province != null)
            {
                NewProvince = province;
            }
        }

        // 🔹 Country: match by Id into the Countries collection
        if (SelectedNewSite.CountryId.HasValue && Countries is { Count: > 0 })
        {
            var country = Countries.FirstOrDefault(c => c.CountryId == SelectedNewSite.CountryId.Value);

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

        try
        {
            var sites = await SiteService.LookupSitesForCompanyAsync(
                SelectedNewCompany.CompanyId,
                term: "",
                CancellationToken.None);

            NewSiteSuggestions.Clear();

            if (sites != null)
            {
                foreach (var s in sites.OrderBy(s => s.SiteName))
                    NewSiteSuggestions.Add(s);
            }

            // If we were editing and want to auto-select an existing SiteId
            if (_pendingSelectSiteId.HasValue)
            {
                SelectedNewSite = NewSiteSuggestions
                    .FirstOrDefault(s => s.SiteId == _pendingSelectSiteId.Value);

                _pendingSelectSiteId = null;
            }
            else if (SelectedNewSite != null && !NewSiteSuggestions.Contains(SelectedNewSite))
            {
                SelectedNewSite = null;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Load sites failed: {ex.Message}";
            NewSiteSuggestions.Clear();
            SelectedNewSite = null;
        }
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

    private ProvinceDto? _searchProvince;

    public ProvinceDto? SearchProvince
    {
        get => _searchProvince;
        set
        {
            _searchProvince = value;
            OnPropertyChanged();
        }
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

    private CountryDto? _searchCountry;

    public CountryDto? SearchCountry
    {
        get => _searchCountry;
        set
        {
            _searchCountry = value;
            OnPropertyChanged();
        }
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
        SearchProvince = allProvince;
        OnPropertyChanged(nameof(SearchProvince));
    }
}
