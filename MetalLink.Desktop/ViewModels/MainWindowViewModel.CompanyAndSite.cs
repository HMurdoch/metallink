using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // =====================================================
    // COMPANY & SITE - STATE (bind these in your View)
    // =====================================================

    // Company search inputs
    private string? _companySearchLetter = "ALL";
    public string? CompanySearchLetter
    {
        get => _companySearchLetter;
        set { _companySearchLetter = value; OnPropertyChanged(); }
    }

    private string _companySearchName = string.Empty;
    public string CompanySearchName
    {
        get => _companySearchName;
        set { _companySearchName = value ?? string.Empty; OnPropertyChanged(); }
    }

    // Company results
    public ObservableCollection<CompanyLookupDto> CompanyResults { get; } = new();

    private CompanyLookupDto? _selectedCompany;
    public CompanyLookupDto? SelectedCompany
    {
        get => _selectedCompany;
        set
        {
            if (_selectedCompany == value) return;
            _selectedCompany = value;
            OnPropertyChanged();

            // When company changes, clear sites + details
            SiteResults.Clear();
            SelectedSite = null;

            CompanyEditName = value?.CompanyName ?? string.Empty;
            OnPropertyChanged(nameof(CanUpdateCompany));
            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    // Company edit section
    private string _companyEditName = string.Empty;
    public string CompanyEditName
    {
        get => _companyEditName;
        set
        {
            _companyEditName = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateCompany));
            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    public bool CanUpdateCompany =>
        SelectedCompany != null &&
        !string.IsNullOrWhiteSpace(CompanyEditName) &&
        !string.Equals(CompanyEditName.Trim(), SelectedCompany.CompanyName?.Trim(), StringComparison.Ordinal);

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

    // Site create/edit fields (remember: address belongs to Site)
    private long? _editingSiteId;
    public long? EditingSiteId
    {
        get => _editingSiteId;
        set { _editingSiteId = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSiteEditMode)); }
    }

    public bool IsSiteEditMode => EditingSiteId.HasValue;

    private string _siteName = string.Empty;
    public string SiteName
    {
        get => _siteName;
        set { _siteName = value ?? string.Empty; OnPropertyChanged(); OnPropertyChanged(nameof(CanCreateOrUpdateSite)); (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged(); }
    }

    private string _siteCode = string.Empty;
    public string SiteCode
    {
        get => _siteCode;
        set { _siteCode = value ?? string.Empty; OnPropertyChanged(); }
    }

    private string _addressLine1 = string.Empty;
    public string AddressLine1
    {
        get => _addressLine1;
        set { _addressLine1 = value ?? string.Empty; OnPropertyChanged(); }
    }

    private string _addressLine2 = string.Empty;
    public string AddressLine2
    {
        get => _addressLine2;
        set { _addressLine2 = value ?? string.Empty; OnPropertyChanged(); }
    }

    private string _suburb = string.Empty;
    public string Suburb
    {
        get => _suburb;
        set { _suburb = value ?? string.Empty; OnPropertyChanged(); }
    }

    private string _city = string.Empty;
    public string City
    {
        get => _city;
        set { _city = value ?? string.Empty; OnPropertyChanged(); }
    }

    private string _postalCode = string.Empty;
    public string PostalCode
    {
        get => _postalCode;
        set { _postalCode = value ?? string.Empty; OnPropertyChanged(); }
    }

    // NOTE: Province/Country are in your Lookup partial already (Provinces/Countries + NewProvince/NewCountry).
    // We'll re-use NewProvince/NewCountry for site create/edit to avoid duplicates.

    public bool CanCreateOrUpdateSite =>
        SelectedCompany != null &&
        !string.IsNullOrWhiteSpace(SiteName);

    // =====================================================
    // COMMANDS (CommunityToolkit)
    // =====================================================

    public IAsyncRelayCommand SearchCompaniesCommand { get; private set; } = null!;
    public IAsyncRelayCommand NewCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand UpdateCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand CancelCompanyEditCommand { get; private set; } = null!;

    public IRelayCommand<CompanyLookupDto> EditCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand LoadSitesForCompanyCommand { get; private set; } = null!;

    public IRelayCommand<SiteLookupDto> EditSiteCommand { get; private set; } = null!;
    public IAsyncRelayCommand<SiteLookupDto> DeleteSiteCommand { get; private set; } = null!;
    public IAsyncRelayCommand CreateOrUpdateSiteCommand { get; private set; } = null!;
    public IAsyncRelayCommand CancelSiteEditCommand { get; private set; } = null!;

    /// <summary>
    /// Call this ONCE from your constructor in MWVM.Core.cs
    /// </summary>
    private void InitializeCompanyAndSiteCommands()
    {
            SearchCompaniesCommand = new AsyncRelayCommand(ct => SearchCompaniesAsync(ct));
            NewCompanyCommand = new AsyncRelayCommand(ct => NewCompanyAsync(ct));
            UpdateCompanyCommand = new AsyncRelayCommand(ct => UpdateCompanyAsync(ct), () => CanUpdateCompany);
            CancelCompanyEditCommand = new AsyncRelayCommand(ct => CancelCompanyEditAsync(ct));

            LoadSitesForCompanyCommand = new AsyncRelayCommand(ct => LoadSitesForSelectedCompanyAsync(ct));

            CreateOrUpdateSiteCommand = new AsyncRelayCommand(ct => CreateOrUpdateSiteAsync(ct), () => CanCreateOrUpdateSite);
            CancelSiteEditCommand = new AsyncRelayCommand(ct => CancelSiteEditAsync(ct));
    }

    // =====================================================
    // IMPLEMENTATION (TODO hooks – wire to your services)
    // =====================================================

    private async Task SearchCompaniesAsync(CancellationToken ct = default)
    {
        // TODO: call CompanyService.LookupCompaniesAsync(...) like you do for customers
        // e.g. var items = await CompanyService.LookupCompaniesAsync(term);

        StatusMessage = "[STATUS] Company search not wired yet.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private async Task NewCompanyAsync(CancellationToken ct = default)
    {
        // TODO: clear company edit + set a "new mode" if you want
        SelectedCompany = null;
        CompanyEditName = string.Empty;

        StatusMessage = "[STATUS] New company (UI reset).";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private void OnEditCompany(CompanyLookupDto? company)
    {
        if (company == null) return;
        SelectedCompany = company;
        CompanyEditName = company.CompanyName ?? string.Empty;

        StatusMessage = $"[STATUS] Editing company {company.CompanyId}.";
        Console.WriteLine(StatusMessage);
    }

    private async Task UpdateCompanyAsync(CancellationToken ct = default)
    {
        if (SelectedCompany == null) return;

        // TODO: call API to update company name
        // await CompanyService.UpdateCompanyNameAsync(SelectedCompany.CompanyId, CompanyEditName, ct);

        StatusMessage = "[STATUS] Update company not wired yet.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private async Task CancelCompanyEditAsync(CancellationToken ct = default)
    {
        if (SelectedCompany != null)
            CompanyEditName = SelectedCompany.CompanyName ?? string.Empty;

        StatusMessage = "[STATUS] Company edit cancelled.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private async Task LoadSitesForSelectedCompanyAsync(CancellationToken ct = default)
    {
        if (SelectedCompany == null)
        {
            SiteResults.Clear();
            SelectedSite = null;
            return;
        }

        // TODO: call SiteService.LookupSitesForCompanyAsync(SelectedCompany.CompanyId, "")
        StatusMessage = "[STATUS] Load sites not wired yet.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private void LoadSelectedSiteIntoEditFields(SiteLookupDto? site)
    {
        if (site == null)
        {
            EditingSiteId = null;
            SiteName = "";
            SiteCode = "";
            AddressLine1 = "";
            AddressLine2 = "";
            Suburb = "";
            City = "";
            PostalCode = "";
            return;
        }

        EditingSiteId = site.SiteId;

        SiteName = site.SiteName ?? "";
        SiteCode = site.SiteCode ?? "";
        AddressLine1 = site.AddressLine1 ?? "";
        AddressLine2 = site.AddressLine2 ?? "";
        Suburb = site.Suburb ?? "";
        City = site.City ?? "";
        PostalCode = site.PostalCode ?? "";

        // Province/Country: re-use your existing NewProvince/NewCountry collections,
        // and set them based on site.ProvinceId/site.CountryId if those exist on SiteLookupDto.
        // (You already have UpdateNewLocationFromSelectedSite() logic elsewhere.)
    }

    private void OnEditSite(SiteLookupDto? site)
    {
        if (site == null) return;
        SelectedSite = site;

        StatusMessage = $"[STATUS] Editing site {site.SiteId}.";
        Console.WriteLine(StatusMessage);
    }

    private async Task CreateOrUpdateSiteAsync(CancellationToken ct = default)
    {
        if (SelectedCompany == null)
        {
            StatusMessage = "[STATUS] Select a company first.";
            Console.WriteLine(StatusMessage);
            return;
        }

        // TODO: call API to create or update site.
        // If EditingSiteId.HasValue => update, else create.

        StatusMessage = "[STATUS] Create/Update site not wired yet.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private async Task OnDeleteSiteAsync(SiteLookupDto? site, CancellationToken ct = default)
    {
        if (site == null) return;

        // TODO: call API to delete site
        StatusMessage = "[STATUS] Delete site not wired yet.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private async Task CancelSiteEditAsync(CancellationToken ct = default)
    {
        // Reset edit fields from SelectedSite
        LoadSelectedSiteIntoEditFields(SelectedSite);

        StatusMessage = "[STATUS] Site edit cancelled.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }
}
