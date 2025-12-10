using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using MetalLink.Shared.Provinces;
using MetalLink.Shared.Locations;

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
    // SEARCH CUSTOMERS – COMPANY LETTER FILTER + SITE
    // =====================================================

    // ----- Company: master list + letter filters -----

    private readonly ObservableCollection<CompanyLookupDto> _allCompanies        = new();
    private readonly ObservableCollection<string>           _companyLetterFilters = new();

    private bool _companyLettersLoaded;
    private bool _companyLettersLoading;

    /// <summary>
    /// Letters used by the "ALL / A / B / C / …" filter ComboBox.
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

    /// <summary>
    /// Companies matching the chosen letter (or ALL).
    /// Bound to the second "Company" ComboBox.
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
    /// Load all companies from the API, build the letter list and
    /// apply the initial "ALL" filter.
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

        // Default to ALL on first load
        if (SelectedCompanyLetter == null)
            SelectedCompanyLetter = "ALL";
        else
            ApplyCompanyLetterFilter();
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
                char.ToUpperInvariant(c.CompanyName[0]) == ch);
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
    // CREATE CUSTOMER: Company & Site typeahead
    // =====================================================

    private ObservableCollection<CompanyLookupDto> _newCompanySuggestions = new();
    public ObservableCollection<CompanyLookupDto> NewCompanySuggestions
    {
        get => _newCompanySuggestions;
        set { _newCompanySuggestions = value; OnPropertyChanged(); }
    }

    private string _newCompanyFilterText = string.Empty;
    public string NewCompanyFilterText
    {
        get => _newCompanyFilterText;
        set
        {
            _newCompanyFilterText = value;
            OnPropertyChanged();

            if (_newCompanyFilterText.Length >= 2)
            {
                _ = RefreshNewCompanySuggestionsAsync();
            }
            else
            {
                NewCompanySuggestions.Clear();
            }
        }
    }

    private CompanyLookupDto? _selectedNewCompany;
    public CompanyLookupDto? SelectedNewCompany
    {
        get => _selectedNewCompany;
        set
        {
            _selectedNewCompany = value;
            OnPropertyChanged();

            if (value != null)
            {
                NewCompanyName = value.CompanyName;
                _ = RefreshNewSiteSuggestionsAsync();
            }
            else
            {
                NewCompanyName = null;
                NewSiteSuggestions.Clear();
                SelectedNewSite = null;
            }
        }
    }

    private async Task RefreshNewCompanySuggestionsAsync()
    {
        var term    = NewCompanyFilterText;
        var results = await CompanyService.LookupCompaniesAsync(term);

        if (!string.Equals(NewCompanyFilterText, term, StringComparison.OrdinalIgnoreCase))
            return;

        NewCompanySuggestions.Clear();

        if (results != null)
        {
            foreach (var c in results)
                NewCompanySuggestions.Add(c);
        }
    }

    private ObservableCollection<SiteLookupDto> _newSiteSuggestions = new();
    public ObservableCollection<SiteLookupDto> NewSiteSuggestions
    {
        get => _newSiteSuggestions;
        set { _newSiteSuggestions = value; OnPropertyChanged(); }
    }

    private string _newSiteFilterText = string.Empty;
    public string NewSiteFilterText
    {
        get => _newSiteFilterText;
        set
        {
            _newSiteFilterText = value;
            OnPropertyChanged();

            if (SelectedNewCompany != null && _newSiteFilterText.Length >= 1)
            {
                _ = RefreshNewSiteSuggestionsAsync();
            }
        }
    }

    private SiteLookupDto? _selectedNewSite;
    public SiteLookupDto? SelectedNewSite
    {
        get => _selectedNewSite;
        set
        {
            _selectedNewSite = value;
            OnPropertyChanged();

            if (value != null)
            {
                // At create time we will use SelectedNewSite.SiteId when calling API
            }
        }
    }

    private async Task RefreshNewSiteSuggestionsAsync()
    {
        if (SelectedNewCompany == null) return;

        var term    = NewSiteFilterText;
        var results = await SiteService.LookupSitesForCompanyAsync(
            SelectedNewCompany.CompanyId,
            term);

        if (!string.Equals(NewSiteFilterText, term, StringComparison.OrdinalIgnoreCase))
            return;

        NewSiteSuggestions.Clear();

        if (results != null)
        {
            foreach (var s in results)
                NewSiteSuggestions.Add(s);
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
