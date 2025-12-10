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

            ApplyCompanyLetterFilter();
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
                // Used by your search handler
                SearchCompanyNameText = value.CompanyName;

                // Enable & load sites for this company
                IsSearchSiteEnabled = true;
                _ = LoadSitesForSelectedCompanyAsync();
            }
            else
            {
                SearchCompanyNameText = string.Empty;

                IsSearchSiteEnabled = false;
                SearchSiteIdText    = string.Empty;

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

    private void OnEditCustomer(Shared.Customers.CustomerDto? customer)
    {
        if (customer == null)
            return;

        EditingCustomerId = customer.CustomerId;
        IsEditMode        = true;

        // Basic fields
        NewFirstName      = customer.FirstName;
        NewLastName       = customer.LastName;
        NewIdNumber       = customer.IdNumber;
        NewAccountNumber  = customer.AccountNumber;
        NewPriceCode      = customer.PriceCode;
        NewPhoneNumber    = customer.PhoneNumber;
        NewMobileNumber   = customer.MobileNumber;
        NewEmail          = customer.Email;
        NewAddressLine1   = customer.AddressLine1;
        NewAddressLine2   = customer.AddressLine2;
        NewSuburb         = customer.Suburb;
        NewCity           = customer.City;
        NewPostalCode     = customer.PostalCode;

        // Company / site mode
        NewIsCompany = true;  // editing from grid implies it's a company customer

        // Match company from our lookup cache (by CompanyId if present)
        var company = _allCompanies.FirstOrDefault(c => c.CompanyId == customer.CompanyId);

        if (company != null)
        {
            var letter = char.ToUpperInvariant(company.CompanyName?.FirstOrDefault() ?? 'A');
            var letterStr = letter.ToString();

            if (!CompanyLetterFilters.Contains(letterStr))
                letterStr = "ALL";

            SelectedNewCompanyLetter = letterStr;
            SelectedNewCompany       = company;
        }
        else
        {
            SelectedNewCompanyLetter = "ALL";
            SelectedNewCompany       = null;
        }

        // Load sites for that company and select correct one
        _ = LoadNewSitesAndSelectAsync(customer.SiteId);
    }

    private async Task OnDeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null)
            return;

        // TODO: show your confirm dialog here

        await _customerService.SoftDeleteCustomerAsync(customer.CustomerId);

        CustomerSearchResults.Remove(customer);
        StatusMessage = $"Customer {customer.FullName} was deleted (soft delete).";
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

    private void ClearNewCustomerForm()
    {
        EditingCustomerId = null;
        IsEditMode        = false;

        NewFirstName     = string.Empty;
        NewLastName      = string.Empty;
        NewIdNumber      = string.Empty;
        NewAccountNumber = string.Empty;
        NewPriceCode     = string.Empty;
        NewPhoneNumber   = string.Empty;
        NewMobileNumber  = string.Empty;
        NewEmail         = string.Empty;
        NewAddressLine1  = string.Empty;
        NewAddressLine2  = string.Empty;
        NewSuburb        = string.Empty;
        NewCity          = string.Empty;
        NewPostalCode    = string.Empty;

        NewIsCompany           = false;
        SelectedNewCompanyLetter = "ALL";
        SelectedNewCompany     = null;
        NewSiteSuggestions.Clear();
        SelectedNewSite        = null;
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
            AddressLine1  = NewAddressLine1,
            AddressLine2  = NewAddressLine2,
            Suburb        = NewSuburb,
            City          = NewCity,
            PostalCode    = NewPostalCode,
            IsCompany     = NewIsCompany,

            // We KNOW these are non-null if NewIsCompany is true
            // because of the validation above.
            CompanyId = (long)(SelectedNewCompany != null
                ? SelectedNewCompany.CompanyId
                : (long?)null),   // will be null for non-company customers

            SiteId = SelectedNewSite != null
                ? SelectedNewSite.SiteId
                : (long?)null
        };

        await _customerService.UpdateCustomerAsync(dto);

        // Refresh / update in current grid
        var existing = CustomerSearchResults
            .FirstOrDefault(c => c.CustomerId == dto.CustomerId);

        if (existing != null)
        {
            existing.FirstName     = dto.FirstName;
            existing.LastName      = dto.LastName;
            existing.FullName      = $"{dto.FirstName} {dto.LastName}".Trim();
            existing.AccountNumber = dto.AccountNumber;
            existing.CompanyName   = SelectedNewCompany?.CompanyName;
            existing.MobileNumber  = dto.MobileNumber;
            existing.Email         = dto.Email;
        }

        ClearNewCustomerForm();
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

        StatusMessage = $"Logging ticket for customer {customer.FullName} ({customer.CustomerId:D8}).";
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

            if (value != null)
            {
                // This string is what CreateCustomerAsync uses.
                NewCompanyName = value.CompanyName;

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

            if (value != null)
            {
                // CreateCustomerAsync already has access to SelectedNewSite
                // to determine which site the customer belongs to.
            }
        }
    }

    // Which customer (if any) are we editing?
    private long? _editingCustomerId;
    public long? EditingCustomerId
    {
        get => _editingCustomerId;
        set { _editingCustomerId = value; OnPropertyChanged(); }
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
            OnPropertyChanged(nameof(IsCreateMode));
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

    public async Task LoadProvincesAsync()
    {
        var items = await ProvinceService.GetAllAsync();
        Provinces.Clear();

        if (items != null)
        {
            foreach (var p in items)
                Provinces.Add(p);
        }
    }
}
