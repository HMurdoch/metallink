using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using MetalLink.Shared.Customers;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Net.Http;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Buyers;
using System.IO;
using Avalonia.Media.Imaging;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private SiteService? _siteService;

    private SiteService SiteService =>
        _siteService ??= new SiteService(_apiClient);

    private long? _pendingSelectSiteId;

    private async Task LoadNewSitesAndSelectAsync(long? siteId)
    {
        NewSiteSuggestions.Clear();

        if (SelectedNewCompany == null)
        {
            SelectedNewSite = null;
            return;
        }

        try
        {
            var sites = await SiteService.LookupSitesForCompanyAsync(
                SelectedNewCompany.CompanyId,
                term: string.Empty,
                CancellationToken.None);

            if (sites != null)
            {
                foreach (var s in sites.OrderBy(s => s.SiteName))
                    NewSiteSuggestions.Add(s);
            }

            SelectedNewSite = siteId.HasValue
                ? NewSiteSuggestions.FirstOrDefault(s => s.SiteId == siteId.Value)
                : null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Load sites failed: {ex.Message}";
            NewSiteSuggestions.Clear();
            SelectedNewSite = null;
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
            StatusMessage = "[STATUS] Load sites failed: " + ex.Message;
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
            IsSearchSiteEnabled = false;
        }
    }

    private void AddCompanyToCachesAndSelect(CompanyLookupDto createdCompany)
    {
        // 1) Add/update in master cache
        var existing = _allCompanies.FirstOrDefault(c => c.CompanyId == createdCompany.CompanyId);
        if (existing != null)
        {
            var idx = _allCompanies.IndexOf(existing);
            if (idx >= 0) _allCompanies[idx] = createdCompany;
        }
        else
        {
            _allCompanies.Add(createdCompany);
        }

        // 2) Keep master cache sorted (optional but helps)
        var sorted = _allCompanies.OrderBy(c => c.CompanyName).ToList();
        _allCompanies.Clear();
        foreach (var c in sorted) _allCompanies.Add(c);

        // 3) Rebuild letter list (simple rebuild)
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

        _companyLettersLoaded = true;
        _companyLettersLoading = false;

        // 4) Switch the letter filter to the new company's letter
        var first = createdCompany.CompanyName?.FirstOrDefault();
        var letterStr = first.HasValue ? char.ToUpperInvariant(first.Value).ToString() : "ALL";
        if (!CompanyLetterFilters.Contains(letterStr))
            letterStr = "ALL";

        SelectedCompanyLetter = letterStr;

        // 5) Refresh both the dropdown + results grid from the cache
        ApplyCompanyLetterFilter();

        CompanyResults.Clear();
        foreach (var c in SearchCompanySuggestions.OrderBy(x => x.CompanyName))
            CompanyResults.Add(c);

        // 6) Select the created company in BOTH selectors
        SelectedSearchCompany = SearchCompanySuggestions.FirstOrDefault(c => c.CompanyId == createdCompany.CompanyId);
        SelectedCompany = CompanyResults.FirstOrDefault(c => c.CompanyId == createdCompany.CompanyId);
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

    private void ClearCompanyEditor()
    {
        EditingCompanyId = null; // if you have it; otherwise just use a bool flag
        CompanyEditName = string.Empty;
        CompanyVatNumber = string.Empty;

        // only relevant for create-mode
        CompanyEditName = string.Empty;
        CompanyVatNumber = string.Empty;
        CompanyFormInitialSiteName = string.Empty;

        (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        (CreateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private string _originalCompanyName = "";
    private string? _originalVatNumber = null;

    public bool IsCompanyEditMode => EditingCompanyId.HasValue;
    public bool IsCompanyCreateMode => !EditingCompanyId.HasValue;

    public bool CanCreateCompany =>
        IsCompanyCreateMode
        && !string.IsNullOrWhiteSpace(CompanyEditName)
        && !string.IsNullOrWhiteSpace(CompanyFormInitialSiteName);

    public bool CanUpdateCompany
    {
        get
        {
            if (!IsCompanyEditMode) return false;
            if (SelectedCompany == null) return false;

            var name = (CompanyEditName ?? "").Trim();
            var vat  = string.IsNullOrWhiteSpace(CompanyVatNumber) ? null : CompanyVatNumber.Trim();

            if (string.IsNullOrWhiteSpace(name)) return false;

            var changedName = !string.Equals(name, _originalCompanyName, StringComparison.Ordinal);
            var changedVat  = !string.Equals(vat, _originalVatNumber, StringComparison.Ordinal);

            return changedName || changedVat;
        }
    }

    public bool HasSiteFormSuccess => !string.IsNullOrWhiteSpace(SiteFormSuccess);

    private void ClearSiteFormMessages()
    {
        SiteFormError = "";
        SiteFormSuccess = "";
    }

    public bool IsSiteEditMode => EditingSiteId.HasValue;

    public bool CanCreateSite =>
        SelectedCompany != null && !string.IsNullOrWhiteSpace(NewSiteCreateName);

    public bool CanCreateOrUpdateSite =>
        SelectedCompany != null &&
        !string.IsNullOrWhiteSpace(SiteName);

    public string SiteSaveButtonText => IsSiteEditMode ? "Update" : "Create";

    public IAsyncRelayCommand CreateSiteForSelectedCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand SearchCompaniesCommand { get; private set; } = null!;
    public IAsyncRelayCommand NewCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand UpdateCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand CancelCompanyEditCommand { get; private set; } = null!;
    public IAsyncRelayCommand LoadSitesForCompanyCommand { get; private set; } = null!;
    public IRelayCommand<SiteLookupDto> EditSiteCommand { get; private set; } = null!;
    public IAsyncRelayCommand<SiteLookupDto> DeleteSiteCommand { get; private set; } = null!;
    public IAsyncRelayCommand CreateOrUpdateSiteCommand { get; private set; } = null!;
    public IAsyncRelayCommand CancelSiteEditCommand { get; private set; } = null!;
    public IAsyncRelayCommand CreateCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand<CompanyLookupDto> ShowSitesForCompanyCommand { get; private set; } = null!;
    public IRelayCommand<CompanyLookupDto> EditCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand<CompanyLookupDto> DeleteCompanyCommand { get; private set; } = null!;
    public IAsyncRelayCommand ClearCompanyFormCommand { get; private set; } = null!;
    public IAsyncRelayCommand UpdateCompany2Command { get; private set; } = null!;
    public IRelayCommand ClearSiteFormCommand { get; private set; } = null!;


    /// <summary>
    /// Call this ONCE from your constructor in MWVM.Core.cs
    /// </summary>
    private void InitializeCompanyAndSiteCommands()
    {
            SearchCompaniesCommand = new AsyncRelayCommand(ct => SearchCompaniesAsync(ct));
            NewCompanyCommand = new AsyncRelayCommand(ct => NewCompanyAsync(ct));
            UpdateCompanyCommand = new AsyncRelayCommand(ct => UpdateCompanyAsync(ct), () => CanUpdateCompany);
            CancelCompanyEditCommand = new AsyncRelayCommand(ct => CancelCompanyEditAsync(ct));
            LoadSitesForCompanyCommand = new AsyncRelayCommand(ct => LoadSitesForSelectedCompanyResultsAsync(ct));
            CreateOrUpdateSiteCommand = new AsyncRelayCommand(ct => CreateOrUpdateSiteAsync(ct), () => CanCreateOrUpdateSite);
            CancelSiteEditCommand = new AsyncRelayCommand(ct => CancelSiteEditAsync(ct));
            CreateCompanyCommand = new AsyncRelayCommand(ct => CreateCompanyAsync(ct), () => CanCreateCompany);
            ShowSitesForCompanyCommand = new AsyncRelayCommand<CompanyLookupDto>(ShowSitesForCompanyAsync);
            CreateSiteForSelectedCompanyCommand = new AsyncRelayCommand(ct => CreateSiteForSelectedCompanyAsync(ct), () => CanCreateSite);
            EditCompanyCommand = new RelayCommand<CompanyLookupDto>(OnEditCompany);
            DeleteCompanyCommand = new AsyncRelayCommand<CompanyLookupDto>(DeleteCompanyAsync);
            EditSiteCommand = new RelayCommand<SiteLookupDto>(OnEditSite);
            DeleteSiteCommand = new AsyncRelayCommand<SiteLookupDto>(OnDeleteSiteAsync);
            ClearCompanyFormCommand = new AsyncRelayCommand(ct => ClearCompanyFormAsync(ct));
            UpdateCompany2Command = new AsyncRelayCommand(ct => UpdateCompanyAsync(ct), () => CanUpdateCompany);
            ClearSiteFormCommand = new RelayCommand(ClearSiteForm);

            InitializeSiteDocumentCommands();

            SitePaginationViewModel.PageChanged += (s, e) => UpdatePagedSiteResults();
    }

    // =====================================================
    // IMPLEMENTATION (TODO hooks – wire to your services)
    // =====================================================

    private async Task SearchCompaniesAsync(CancellationToken ct = default)
    {
        await Task.Yield(); // Avoid CS1998
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            StatusMessage = "Searching companies...";

            // Start from cached master list
            var query = _allCompanies.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(CompanySearchText))
            {
                // Explicitly filter by name if text is provided
                query = query.Where(c => 
                    !string.IsNullOrWhiteSpace(c.CompanyName) && 
                    c.CompanyName.Contains(CompanySearchText, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var letter = (SelectedCompanyLetter ?? "ALL").Trim();
                if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
                {
                    var ch = char.ToUpperInvariant(letter[0]);
                    query = query.Where(c =>
                        !string.IsNullOrWhiteSpace(c.CompanyName) &&
                        char.ToUpperInvariant(c.CompanyName![0]) == ch);
                }
            }

            // Populate results grid (Alphabetical)
            CompanyResults.Clear();
            foreach (var c in query.OrderBy(c => c.CompanyName))
                CompanyResults.Add(c);

            PaginationViewModel.SetTotalRecords(CompanyResults.Count);
            UpdatePagedCompanyResults();

            // Auto-expand next panels
            CompanyIsSearchResultsExpanded = true;
            CompanyIsCreateEditExpanded = true;

            StatusMessage = $"[STATUS] Found {CompanyResults.Count} company(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Company search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdatePagedCompanyResults()
    {
        PagedCompanyResults.Clear();
        var paged = CompanyResults
            .Skip(PaginationViewModel.GetSkip())
            .Take(PaginationViewModel.GetTake());
        foreach (var c in paged) PagedCompanyResults.Add(c);
    }

    private void ClearSiteForm()
    {
        EditingSiteId = null;
        SelectedSite = null;

        SiteName = "";
        
        // Requirement: SITE-XX + 1 auto-generation
        int nextNum = 1;
        if (SiteResults != null && SiteResults.Count > 0)
        {
            // Extract numbers from SITE-N pattern
            var nums = SiteResults
                .Select(s => s.SiteCode)
                .Where(c => !string.IsNullOrEmpty(c) && c.StartsWith("SITE-", StringComparison.OrdinalIgnoreCase))
                .Select(c => int.TryParse(c.Substring(5), out var n) ? n : 0)
                .ToList();

            if (nums.Any())
            {
                nextNum = nums.Max() + 1;
            }
            else
            {
                // Fallback: if no SITE-N exists, use count + 1
                nextNum = SiteResults.Count + 1;
            }
        }
        
        SiteCode = $"SITE-{nextNum}";

        AddressLine1 = "";
        AddressLine2 = "";
        Suburb = "";
        City = "";
        PostalCode = "";

        // Clear document bitmaps
        CipcDocument = null;
        TradingLicense = null;
        VatDocument = null;
        TaxDocument = null;
        BbeeDocument = null;

        SelectedCipcDocument = null;
        SelectedTradingLicense = null;
        SelectedVatDocument = null;
        SelectedTaxDocument = null;
        SelectedBbeeDocument = null;

        // Clear pending data
        _pendingCipcData = null;
        _pendingTradingData = null;
        _pendingVatData = null;
        _pendingTaxData = null;
        _pendingBbeeData = null;

        // Default Province and Country
        SetDefaultProvinceAndCountryForSite();

        OnPropertyChanged(nameof(SiteCode));
        OnPropertyChanged(nameof(SiteSaveButtonText));
        OnPropertyChanged(nameof(IsSiteEditMode));
        (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

        UploadCipcDocumentCommand?.NotifyCanExecuteChanged();
        UploadTradingLicenseCommand?.NotifyCanExecuteChanged();
        UploadVatDocumentCommand?.NotifyCanExecuteChanged();
        UploadTaxDocumentCommand?.NotifyCanExecuteChanged();
        UploadBbeeDocumentCommand?.NotifyCanExecuteChanged();
        CommitSiteDocumentsCommand?.NotifyCanExecuteChanged();
    }

    private void SetDefaultProvinceAndCountryForSite()
    {
        // Gauteng default
        var gp = Provinces.FirstOrDefault(p =>
            string.Equals(p.ProvinceName, "Gauteng", StringComparison.OrdinalIgnoreCase));
        if (gp != null) SelectedProvince = gp;

        // South Africa default
        var za = Countries.FirstOrDefault(c =>
            string.Equals(c.CountryName, "South Africa", StringComparison.OrdinalIgnoreCase));
        if (za != null) SelectedCountry = za;
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

    private async Task CreateCompanyAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (!CanCreateCompany) return;

        IsBusy = true;

         try
        {
            StatusMessage = "[STATUS] Creating company...";

            var company = await _app.CompanyAndSiteService.CreateCompanyAsync(
                new CompanyDto
                {
                    CompanyName = CompanyEditName.Trim(),
                    VatNumber = string.IsNullOrWhiteSpace(CompanyVatNumber) ? null : CompanyVatNumber.Trim(),
                    IsActive = true
                },
                ct);

            if (company == null)
                throw new Exception("CreateCompany returned null.");

            var site = await _app.CompanyAndSiteService.CreateSiteAsync(
                new SiteDto
                {
                    CompanyId = company!.CompanyId,
                    SiteName = CompanyFormInitialSiteName.Trim(),
                    SiteCode = "SITE-1",
                    IsActive = true,
                    CountryId = 1 // Default South Africa
                }, ct);

            if (site == null)
                throw new Exception("CreateSite returned null.");

            // Clear UI
            CompanyEditName = string.Empty;
            CompanyVatNumber = string.Empty;
            CompanyFormInitialSiteName = string.Empty;

            StatusMessage = $"[STATUS] Created company {company.CompanyName} and site {site.SiteName}.";

            // Convert to lookup dto
            var lookup = new CompanyLookupDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                VatNumber = company.VatNumber,
                IsActive = company.IsActive
            };

            AddCompanyToCachesAndSelect(lookup);
            
            // Ensure the grid selection is the created company
            SelectedCompany = lookup;
            
            await LoadSitesForSelectedCompanyResultsAsync(ct);
            
            // Auto-select the newly created initial site and enter edit mode
            var siteLookup = SiteResults.FirstOrDefault(s => s.SiteId == site.SiteId);
            if (siteLookup != null)
            {
                SelectedSite = siteLookup;
                // details are populated by SelectedSite setter -> LoadSelectedSiteIntoEditFields
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Create failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnEditCompany(CompanyLookupDto? company)
    {
        if (company == null) return;

        SelectedCompany = company;           // keeps grid selection consistent
        EditingCompanyId = company.CompanyId;

        _originalCompanyName = company.CompanyName?.Trim() ?? "";
        _originalVatNumber = company.VatNumber?.Trim();

        CompanyEditName = company.CompanyName ?? "";
        CompanyVatNumber = company.VatNumber ?? "";

        IsCompanyInitialSiteVisible = false;

        OnPropertyChanged(nameof(CanUpdateCompany));
        (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();

        // only if your dto has it; otherwise leave blank
        // CompanyFormVatNumber = company.VatNumber ?? "";

        // create-only field; don’t use it in edit
        CompanyFormInitialSiteName = "";

        StatusMessage = $"[STATUS] Editing company {company.CompanyId}.";
    }

    private async Task UpdateCompanyAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (SelectedCompany == null) return;
        if (!CanUpdateCompany) return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Updating company...";

            var dto = new CompanyDto
            {
                CompanyId = SelectedCompany.CompanyId,
                CompanyName = CompanyEditName.Trim(),
                VatNumber = string.IsNullOrWhiteSpace(CompanyVatNumber) ? null : CompanyVatNumber.Trim(),
                IsActive = true
            };

            await _app.CompanyAndSiteService.UpdateCompanyAsync(SelectedCompany.CompanyId, dto, ct);

            // Update master cache
            var cache = _allCompanies?.FirstOrDefault(x => x.CompanyId == dto.CompanyId);
            if (cache != null)
            {
                cache.CompanyName = dto.CompanyName;
                cache.VatNumber = dto.VatNumber;
            }

            // Update Results list
            var resultRow = CompanyResults.FirstOrDefault(x => x.CompanyId == dto.CompanyId);
            if (resultRow != null)
            {
                resultRow.CompanyName = dto.CompanyName;
                resultRow.VatNumber = dto.VatNumber;
            }

            // re-render grid
            UpdatePagedCompanyResults();
            
            StatusMessage = $"[STATUS] Company updated: {dto.CompanyName}";

            // User requirement: after update, return to Create mode (show Create button and Initial Site Name)
            SelectedCompany = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Update failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            (UpdateCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private Task ClearCompanyFormAsync(CancellationToken ct = default)
    {
        SelectedCompany = null; // This will trigger the setter logic to reset fields and show Initial Site Name
        
        StatusMessage = "[STATUS] Company form cleared.";
        return Task.CompletedTask;
    }

    private async Task DeleteCompanyAsync(CompanyLookupDto? company, CancellationToken ct = default)
    {
        if (company == null) return;
        if (IsBusy) return;

        var ok = await ConfirmAsync($"Are you sure you want to delete - {company.CompanyName} ?");
        if (!ok) return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Deleting company...";
            await _app.CompanyAndSiteService.DeleteCompanyAsync(company.CompanyId, ct);

            var row = CompanyResults.FirstOrDefault(x => x.CompanyId == company.CompanyId);
            if (row != null) CompanyResults.Remove(row);

            if (SelectedCompany?.CompanyId == company.CompanyId)
            {
                SelectedCompany = null;
                EditingCompanyId = null;
                SiteResults.Clear();
                CompanyFormName = "";
                CompanyFormVatNumber = "";
                CompanyFormInitialSiteName = "";
            }

            StatusMessage = "[STATUS] Company deleted (soft).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    private async Task CancelCompanyEditAsync(CancellationToken ct = default)
    {
        if (SelectedCompany != null)
            CompanyEditName = SelectedCompany.CompanyName ?? string.Empty;

        StatusMessage = "[STATUS] Company edit cancelled.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private void LoadSelectedSiteIntoEditFields(SiteLookupDto? site)
    {
        if (site == null)
        {
            ClearSiteForm();
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

        // Explicitly load provinces/countries if empty
        if (Provinces.Count == 0) _ = LoadProvincesAsync();
        if (Countries.Count == 0) InitializeCountries();

        if (site.ProvinceId > 0)
            SelectedProvince = Provinces.FirstOrDefault(p => p.ProvinceId == site.ProvinceId);

        if (site.CountryId > 0)
            SelectedCountry = Countries.FirstOrDefault(c => c.CountryId == site.CountryId);

        // fallback defaults if null
        if (SelectedProvince == null || SelectedCountry == null)
            SetDefaultProvinceAndCountryForSite();

        OnPropertyChanged(nameof(SiteSaveButtonText));
        OnPropertyChanged(nameof(IsSiteEditMode));
        (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        
        UploadCipcDocumentCommand?.NotifyCanExecuteChanged();
        UploadTradingLicenseCommand?.NotifyCanExecuteChanged();
        UploadVatDocumentCommand?.NotifyCanExecuteChanged();
        UploadTaxDocumentCommand?.NotifyCanExecuteChanged();
        UploadBbeeDocumentCommand?.NotifyCanExecuteChanged();
        CommitSiteDocumentsCommand?.NotifyCanExecuteChanged();

        // Load Site Documents
        _ = LoadSelectedSiteDocumentsAsync(site);
    }

    private async Task LoadSelectedSiteDocumentsAsync(SiteLookupDto site)
    {
        // Clear existing local preview bitmaps
        CipcDocument = null;
        TradingLicense = null;
        VatDocument = null;
        TaxDocument = null;
        BbeeDocument = null;
        
        SelectedCipcDocument = null;
        SelectedTradingLicense = null;
        SelectedVatDocument = null;
        SelectedTaxDocument = null;
        SelectedBbeeDocument = null;

        // Clear pending upload data as we are loading a fresh site
        _pendingCipcData = null;
        _pendingTradingData = null;
        _pendingVatData = null;
        _pendingTaxData = null;
        _pendingBbeeData = null;

        try
        {
            var cipc = await SiteService.DownloadSiteDocumentAsync(site.SiteId, "cipc");
            var trading = await SiteService.DownloadSiteDocumentAsync(site.SiteId, "trading");
            // var cipro = await SiteService.DownloadSiteDocumentAsync(site.SiteId, "cipro"); // cipro renamed to vat in DB but let's stick to the 5 reqs
            var vat = await SiteService.DownloadSiteDocumentAsync(site.SiteId, "vat");
            var tax = await SiteService.DownloadSiteDocumentAsync(site.SiteId, "tax");
            var bbee = await SiteService.DownloadSiteDocumentAsync(site.SiteId, "bbee");

            CipcDocument = LoadBitmapFromBytes(cipc);
            TradingLicense = LoadBitmapFromBytes(trading);
            VatDocument = LoadBitmapFromBytes(vat);
            TaxDocument = LoadBitmapFromBytes(tax);
            BbeeDocument = LoadBitmapFromBytes(bbee);

            // Mirror to edit panel
            SelectedCipcDocument = CipcDocument;
            SelectedTradingLicense = TradingLicense;
            SelectedVatDocument = VatDocument;
            SelectedTaxDocument = TaxDocument;
            SelectedBbeeDocument = BbeeDocument;
        }
        catch { /* ignore missing */ }
    }

    public IAsyncRelayCommand UploadCipcDocumentCommand { get; private set; } = null!;
    public IAsyncRelayCommand UploadTradingLicenseCommand { get; private set; } = null!;
    public IAsyncRelayCommand UploadVatDocumentCommand { get; private set; } = null!;
    public IAsyncRelayCommand UploadTaxDocumentCommand { get; private set; } = null!;
    public IAsyncRelayCommand UploadBbeeDocumentCommand { get; private set; } = null!;
    public IAsyncRelayCommand CommitSiteDocumentsCommand { get; private set; } = null!;

    private void InitializeSiteDocumentCommands()
    {
        UploadCipcDocumentCommand = new AsyncRelayCommand(() => GenerateMockSiteDocAsync("cipc"), () => IsSiteEditMode);
        UploadTradingLicenseCommand = new AsyncRelayCommand(() => GenerateMockSiteDocAsync("trading"), () => IsSiteEditMode);
        UploadVatDocumentCommand = new AsyncRelayCommand(() => GenerateMockSiteDocAsync("vat"), () => IsSiteEditMode);
        UploadTaxDocumentCommand = new AsyncRelayCommand(() => GenerateMockSiteDocAsync("tax"), () => IsSiteEditMode);
        UploadBbeeDocumentCommand = new AsyncRelayCommand(() => GenerateMockSiteDocAsync("bbee"), () => IsSiteEditMode);
        CommitSiteDocumentsCommand = new AsyncRelayCommand(CommitSiteDocumentsAsync, () => IsSiteEditMode);
    }

    private byte[]? _pendingCipcData;
    private byte[]? _pendingTradingData;
    private byte[]? _pendingVatData;
    private byte[]? _pendingTaxData;
    private byte[]? _pendingBbeeData;

    private async Task GenerateMockSiteDocAsync(string type)
    {
        Console.WriteLine($"[DEBUG] GenerateMockSiteDocAsync called for type: {type}");
        await Task.Yield();
        
        // Mock generation logic with distinct details
        string text = "";
        uint color = 0xFFFFFFFF;

        switch (type.ToLower())
        {
            case "cipc":
                text = "OFFICIAL CIPC DOCUMENT\nRegistered Company Details\nSite ID: " + (SelectedSite?.SiteId ?? 0);
                color = 0xFFE3F2FD; // Light Blue
                break;
            case "trading":
                text = "TRADING LICENSE\nValid for Metal Link Operations\nSite Code: " + (SiteCode ?? "N/A");
                color = 0xFFF1F8E9; // Light Green
                break;
            case "vat":
                text = "VAT REGISTRATION\nValue Added Tax Compliance\nTax Office Ref: " + DateTime.Now.Ticks;
                color = 0xFFF3E5F5; // Light Purple
                break;
            case "tax":
                text = "TAX CLEARANCE\nGood Standing Certificate\nExpiry: " + DateTime.Now.AddYears(1).ToShortDateString();
                color = 0xFFE8EAF6; // Indigo tint
                break;
            case "bbee":
                text = "B-BBEE CERTIFICATE\nLevel Verification\nVerified On: " + DateTime.Now.ToShortDateString();
                color = 0xFFFCE4EC; // Pink tint
                break;
        }

        // Use the helper to generate a bitmap with distinct patterns
        var bytes = GenerateMockImageWithText(type, color);
        
        switch (type.ToLower())
        {
            case "cipc":
                _pendingCipcData = bytes;
                SelectedCipcDocument = LoadBitmapFromBytes(bytes);
                break;
            case "trading":
                _pendingTradingData = bytes;
                SelectedTradingLicense = LoadBitmapFromBytes(bytes);
                break;
            case "vat":
                _pendingVatData = bytes;
                SelectedVatDocument = LoadBitmapFromBytes(bytes);
                break;
            case "tax":
                _pendingTaxData = bytes;
                SelectedTaxDocument = LoadBitmapFromBytes(bytes);
                break;
            case "bbee":
                _pendingBbeeData = bytes;
                SelectedBbeeDocument = LoadBitmapFromBytes(bytes);
                break;
        }

        StatusMessage = $"[STATUS] Generated mock {type.ToUpper()} document.";
    }

    private async Task CommitSiteDocumentsAsync()
    {
        if (SelectedSite == null)
        {
            StatusMessage = "[STATUS] Error: No site selected.";
            return;
        }

        IsBusy = true;
        try
        {
            if (_pendingCipcData != null)
            {
                await SiteService.UploadSiteDocumentAsync(SelectedSite.SiteId, "cipc", _pendingCipcData);
            }
            if (_pendingTradingData != null)
            {
                await SiteService.UploadSiteDocumentAsync(SelectedSite.SiteId, "trading", _pendingTradingData);
            }
            if (_pendingVatData != null)
            {
                await SiteService.UploadSiteDocumentAsync(SelectedSite.SiteId, "vat", _pendingVatData);
            }
            if (_pendingTaxData != null)
            {
                await SiteService.UploadSiteDocumentAsync(SelectedSite.SiteId, "tax", _pendingTaxData);
            }
            if (_pendingBbeeData != null)
            {
                await SiteService.UploadSiteDocumentAsync(SelectedSite.SiteId, "bbee", _pendingBbeeData);
            }

            // Success: clear pending and reload
            _pendingCipcData = null;
            _pendingTradingData = null;
            _pendingVatData = null;
            _pendingTaxData = null;
            _pendingBbeeData = null;

            // Reload to show confirmed from server
            await LoadSelectedSiteDocumentsAsync(SelectedSite);
            StatusMessage = "[STATUS] Site documents committed successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Commit failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private byte[] GenerateMockImageWithText(string type, uint backgroundColor)
    {
        // Simple mock image generation (600x400)
        int width = 600;
        int height = 400;
        var pixels = new uint[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;

        // Add some distinct patterns so we can tell them apart visually
        // 1. A thick border
        uint borderColor = 0xFF444444;
        int borderThickness = 20;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x < borderThickness || x >= width - borderThickness || y < borderThickness || y >= height - borderThickness)
                {
                    pixels[y * width + x] = borderColor;
                }
            }
        }

        // 2. Type-specific patterns
        uint patternColor = 0xFF888888;
        if (type == "cipc")
        {
            // Vertical stripes
            for (int x = 100; x < width; x += 100)
                for (int y = 0; y < height; y++) pixels[y * width + x] = patternColor;
        }
        else if (type == "trading")
        {
            // Horizontal stripes
            for (int y = 100; y < height; y += 100)
                for (int x = 0; x < width; x++) pixels[y * width + x] = patternColor;
        }
        else if (type == "cipro")
        {
            // Diagonal line
            for (int i = 0; i < Math.Min(width, height); i++)
                pixels[i * width + i] = patternColor;
        }
        else if (type == "vat")
        {
            // Box in middle
            for (int y = 150; y < 250; y++)
                for (int x = 250; x < 350; x++) pixels[y * width + x] = patternColor;
        }
        else if (type == "tax")
        {
            // Cross
            for (int i = 0; i < width; i++) pixels[200 * width + i] = patternColor;
            for (int i = 0; i < height; i++) pixels[i * width + 300] = patternColor;
        }
        else if (type == "bbee")
        {
            // Dots
            for (int y = 50; y < height; y += 50)
                for (int x = 50; x < width; x += 50) pixels[y * width + x] = patternColor;
        }

        using (var bitmap = new WriteableBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul))
        {
            using (var locked = bitmap.Lock())
            {
                unsafe
                {
                    uint* ptr = (uint*)locked.Address;
                    for (int i = 0; i < pixels.Length; i++) ptr[i] = pixels[i];
                }
            }

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms);
                return ms.ToArray();
            }
        }
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
        if (IsBusy) return;
        if (SelectedCompany == null)
        {
            StatusMessage = "[STATUS] Select a company first.";
            return;
        }

        if (SelectedProvince == null || SelectedCountry == null)
        {
            StatusMessage = "[STATUS] Please select both Province and Country.";
            return;
        }

        if (!CanCreateOrUpdateSite) return;

        IsBusy = true;
        try
        {
            if (!EditingSiteId.HasValue)
            {
                StatusMessage = "[STATUS] Creating site...";

                await _app.SiteService.CreateSiteAsync(
                    new SiteCreateDto
                    {
                        CompanyId = (int)SelectedCompany.CompanyId,
                        SiteName = SiteName.Trim(),
                        SiteCode = string.IsNullOrWhiteSpace(SiteCode) ? null! : SiteCode.Trim(),
                        AddressLine1 = string.IsNullOrWhiteSpace(AddressLine1) ? null! : AddressLine1.Trim(),
                        AddressLine2 = string.IsNullOrWhiteSpace(AddressLine2) ? null! : AddressLine2.Trim(),
                        Suburb = string.IsNullOrWhiteSpace(Suburb) ? null! : Suburb.Trim(),
                        City = string.IsNullOrWhiteSpace(City) ? null! : City.Trim(),
                        PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode.Trim(),
                        ProvinceId = SelectedProvince?.ProvinceId,
                        CountryId = SelectedCountry?.CountryId,
                        IsActive = true
                    }, ct);

                StatusMessage = "[STATUS] Site created.";
            }
            else
            {
                StatusMessage = "[STATUS] Updating site...";

                await _app.CompanyAndSiteService.UpdateSiteAsync(
                    EditingSiteId.Value,
                    new SiteDto
                    {
                        SiteId = EditingSiteId.Value,
                        CompanyId = SelectedCompany.CompanyId,
                        SiteName = SiteName.Trim(),
                        SiteCode = SiteCode,
                        AddressLine1 = AddressLine1,
                        AddressLine2 = AddressLine2,
                        Suburb = Suburb,
                        City = City,
                        PostalCode = PostalCode,
                        ProvinceId = SelectedProvince?.ProvinceId,
                        CountryId = SelectedCountry?.CountryId,
                        IsActive = true
                    }, ct);

                StatusMessage = "[STATUS] Site updated.";
            }

            await LoadSitesForSelectedCompanyResultsAsync(ct);
            ClearSiteForm();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Save site failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task OnDeleteSiteAsync(SiteLookupDto? site, CancellationToken ct = default)
    {
        if (site == null) return;
        if (IsBusy) return;

        var ok = await ConfirmAsync($"Are you sure you want to delete site '{site.SiteName}'?");
        if (!ok) return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Deleting site...";
            await _app.CompanyAndSiteService.DeleteSiteAsync(site.SiteId, ct);

            // Remove from results grid
            var row = SiteResults.FirstOrDefault(x => x.SiteId == site.SiteId);
            if (row != null) SiteResults.Remove(row);

            // Clear selection if it was the deleted site
            if (SelectedSite?.SiteId == site.SiteId)
            {
                SelectedSite = null;
                ClearSiteForm();
            }

            StatusMessage = "[STATUS] Site deleted (soft).";
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("400"))
        {
            // Handle the validation error from the API
            StatusMessage = "[STATUS] Cannot delete the last active site. A company must have at least one active site.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelSiteEditAsync(CancellationToken ct = default)
    {
        // Reset edit fields from SelectedSite
        LoadSelectedSiteIntoEditFields(SelectedSite);

        StatusMessage = "[STATUS] Site edit cancelled.";
        Console.WriteLine(StatusMessage);
        await Task.CompletedTask;
    }

    private async Task RefreshCompaniesForLetterAsync()
    {
        try
        {
            var term = (SelectedCompanyLetter?.Equals("ALL", StringComparison.OrdinalIgnoreCase) ?? true)
                ? null
                : SelectedCompanyLetter;

            // If your API lookup expects "term", sending "E" will return E companies.
            // If it expects free text, this still works as a prefix filter.
            var items = await _app.CompanyAndSiteService.LookupCompaniesAsync(term);

            // If "ALL" selected, we may want everything.
            // If API returns too broad, we can still enforce letter filtering locally.
            if (!string.IsNullOrWhiteSpace(SelectedCompanyLetter) &&
                !SelectedCompanyLetter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                items = items
                    .Where(c => (c.CompanyName ?? string.Empty)
                        .StartsWith(SelectedCompanyLetter, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            // dropdown suggestions
            SearchCompanySuggestions.Clear();
            foreach (var c in items.OrderBy(x => x.CompanyName))
                SearchCompanySuggestions.Add(c);

            // ✅ ALSO update results grid immediately to match the letter selection
            CompanyResults.Clear();
            foreach (var c in items.OrderBy(x => x.CompanyName))
                CompanyResults.Add(c);

            // Do not auto-select a company; leave selection to the user.
            SelectedSearchCompany = null;
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;
            IsSearchSiteEnabled = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Company refresh failed: {ex.Message}";
        }
    }

    private async Task LoadSitesForSelectedCompanyResultsAsync(CancellationToken ct = default)
    {
        var selected = SelectedCompany;
        if (selected == null)
        {
            Console.WriteLine("[DEBUG] LoadSitesForSelectedCompanyResultsAsync: No company selected.");
            SiteResults.Clear();
            PagedSiteResults.Clear();
            SelectedSite = null;
            return;
        }

        var companyId = selected.CompanyId;
        Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync: CompanyId={companyId} for {selected.CompanyName}");

        try
        {
            StatusMessage = "Loading sites...";

            Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync: Fetching sites from service for company {companyId}...");
            var items = await _app.SiteService.LookupSitesForCompanyAsync(
                companyId,
                term: string.Empty,
                ct) ?? new List<SiteLookupDto>();

            Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync: Service returned {items.Count} items.");

            Dispatcher.UIThread.Post(() => {
                SiteResults.Clear();
                foreach (var s in items.OrderBy(x => x.SiteName))
                {
                    Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync (UI): Processing site: {s.SiteName} (ID: {s.SiteId}, Code: {s.SiteCode})");
                    SiteResults.Add(s);
                }

                Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync (UI): SiteResults collection now has {SiteResults.Count} items.");
                
                Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync (UI): Updating Pagination with total records {SiteResults.Count}");
                
                // Reset to page 1 for a new company fetch
                SitePaginationViewModel.Reset();
                SitePaginationViewModel.SetTotalRecords(SiteResults.Count);
                
                // Log pagination state
                Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync (UI): Pagination State -> CurrentPage: {SitePaginationViewModel.CurrentPage}, TotalPages: {SitePaginationViewModel.TotalPages}, TotalRecords: {SitePaginationViewModel.TotalRecords}");

                UpdatePagedSiteResults();

                // Auto-generate next SITE-XX now that we have the full list
                ClearSiteForm();

                StatusMessage = $"Loaded {SiteResults.Count} site(s).";
                Console.WriteLine($"[DEBUG] LoadSitesForSelectedCompanyResultsAsync (UI): Finished loading sites. SiteResults count: {SiteResults.Count}, PagedSiteResults count: {PagedSiteResults.Count}");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] LoadSitesForSelectedCompanyResultsAsync: {ex}");
            StatusMessage = $"Load sites failed: {ex.Message}";
            SiteResults.Clear();
            PagedSiteResults.Clear();
        }
    }

    private void UpdatePagedSiteResults()
    {
        Console.WriteLine($"[DEBUG] UpdatePagedSiteResults START: SiteResults count={SiteResults.Count}");
        
        var skip = SitePaginationViewModel.GetSkip();
        var take = SitePaginationViewModel.GetTake();
        Console.WriteLine($"[DEBUG] UpdatePagedSiteResults: Pagination params -> Skip={skip}, Take={take}");

        var paged = SiteResults
            .OrderBy(x => x.SiteName)
            .Skip(skip)
            .Take(take)
            .ToList();
        
        Console.WriteLine($"[DEBUG] UpdatePagedSiteResults: LINQ query returned {paged.Count} items to display on current page.");

        // Clear and add on the same thread if we are already on UI thread, or Post if not.
        // However, to be safe and consistent with LoadSitesForSelectedCompanyResultsAsync,
        // we should ensure the collection modification itself is atomic on UI thread.
        Dispatcher.UIThread.Post(() => {
            PagedSiteResults.Clear();
            foreach (var s in paged) 
            {
                // Requirement: hide delete if only 1 site
                s.IsDeleteVisible = SiteResults.Count > 1;
                Console.WriteLine($"[DEBUG] UpdatePagedSiteResults (UI): Adding to PagedSiteResults -> {s.SiteName} (ID: {s.SiteId})");
                PagedSiteResults.Add(s);
            }
            Console.WriteLine($"[DEBUG] UpdatePagedSiteResults END: PagedSiteResults collection now has {PagedSiteResults.Count} items.");
        });
    }


    private async Task ShowSitesForCompanyAsync(CompanyLookupDto? company, CancellationToken ct = default)
    {
        if (company == null) return;

        // selecting it will already trigger LoadSitesForSelectedCompanyResultsAsync()
        SelectedCompany = company;

        // but to be 100% explicit:
        await LoadSitesForSelectedCompanyResultsAsync(ct);
    }

    private async Task CreateSiteForSelectedCompanyAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        if (SelectedCompany == null)
        {
            StatusMessage = "[STATUS] Select a company first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewSiteCreateName))
        {
            StatusMessage = "[STATUS] Site name is required.";
            return;
        }

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Creating site...";

            // Uses your Desktop SiteService (calls POST api/sites)
            var created = await _app.SiteService.CreateSiteAsync(
                new SiteCreateDto
                {
                    CompanyId = (int)SelectedCompany.CompanyId,
                    SiteName = NewSiteCreateName.Trim(),
                    SiteCode = string.IsNullOrWhiteSpace(NewSiteCreateCode) ? null! : NewSiteCreateCode.Trim(),
                    IsActive = true,

                    // use your existing NewCountry/NewProvince if you want,
                    CountryId = NewCountry?.CountryId ?? 1,
                    ProvinceId = NewProvince?.ProvinceId
                },
                ct);

            if (created == null)
                throw new Exception("CreateSite returned null.");

            NewSiteCreateName = "";
            NewSiteCreateCode = "";

            await LoadSitesForSelectedCompanyResultsAsync(ct);
            
            // Auto-select the newly created site and enter edit mode
            SelectedSearchSite = SiteResults.FirstOrDefault(s => s.SiteId == created.SiteId);
            if (SelectedSearchSite != null)
            {
                OnEditSite(SelectedSearchSite);
            }
            
            StatusMessage = $"[STATUS] Created site {created.SiteName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Create site failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            (CreateSiteForSelectedCompanyCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private async Task LoadCompaniesAndLettersAsync()
    {
        try
        {
            var items = await _app.CompanyAndSiteService.LookupCompaniesAsync(string.Empty);

            _allCompanies.Clear();
            foreach (var c in items.OrderBy(c => c.CompanyName))
                _allCompanies.Add(c);

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

            _companyLettersLoaded = true;
            _companyLettersLoading = false;

            // Defaults
            SelectedCompanyLetter ??= "ALL";
            SelectedNewCompanyLetter ??= "ALL";

            // Apply both dropdowns
            ApplyCompanyLetterFilter();
            ApplyNewCompanyLetterFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load company letters: {ex.Message}";
            _companyLettersLoaded = true;
            _companyLettersLoading = false;
        }
    }

    private void ApplyCompanyLetterFilter()
    {
        if (!_companyLettersLoaded) return;
        var selectedId = SelectedSearchCompany?.CompanyId;
        var letter = (SelectedCompanyLetter ?? "ALL").Trim();
        SearchCompanySuggestions.Clear();
        
        IEnumerable<CompanyLookupDto> query = _allCompanies;

        // If search text is provided, filter by name (ignoring letter)
        if (!string.IsNullOrWhiteSpace(CompanySearchText))
        {
            query = query.Where(c => 
                !string.IsNullOrWhiteSpace(c.CompanyName) && 
                c.CompanyName.Contains(CompanySearchText, StringComparison.OrdinalIgnoreCase));
        }
        else if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c => 
                !string.IsNullOrWhiteSpace(c.CompanyName) && 
                char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }

        foreach (var c in query.OrderBy(c => c.CompanyName)) 
            SearchCompanySuggestions.Add(c);

        if (selectedId.HasValue) 
            SelectedSearchCompany = SearchCompanySuggestions.FirstOrDefault(x => x.CompanyId == selectedId.Value);
    }

    private void ApplyCustomerCompanyLetterFilter()
    {
        if (!_companyLettersLoaded) return;
        var selectedId = CustomerSelectedSearchCompany?.CompanyId;
        var letter = (CustomerSelectedCompanyLetter ?? "ALL").Trim();
        CustomerSearchCompanySuggestions.Clear();
        IEnumerable<CompanyLookupDto> query = _allCompanies;
        if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c => !string.IsNullOrWhiteSpace(c.CompanyName) && char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }
        foreach (var c in query.OrderBy(c => c.CompanyName)) CustomerSearchCompanySuggestions.Add(c);
        if (selectedId.HasValue) CustomerSelectedSearchCompany = CustomerSearchCompanySuggestions.FirstOrDefault(x => x.CompanyId == selectedId.Value);
    }

    private void ApplyBuyerCompanyLetterFilter()
    {
        if (!_companyLettersLoaded) return;
        var selectedId = BuyerSelectedSearchCompany?.CompanyId;
        var letter = (BuyerSelectedCompanyLetter ?? "ALL").Trim();
        BuyerSearchCompanySuggestions.Clear();
        IEnumerable<CompanyLookupDto> query = _allCompanies;
        if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c => !string.IsNullOrWhiteSpace(c.CompanyName) && char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }
        foreach (var c in query.OrderBy(c => c.CompanyName)) BuyerSearchCompanySuggestions.Add(c);
        if (selectedId.HasValue) BuyerSelectedSearchCompany = BuyerSearchCompanySuggestions.FirstOrDefault(x => x.CompanyId == selectedId.Value);
    }

    private Task LoadCustomerCompanySuggestionsAsync() { ApplyCustomerCompanyLetterFilter(); return Task.CompletedTask; }
    private Task LoadBuyerCompanySuggestionsAsync() { ApplyBuyerCompanyLetterFilter(); return Task.CompletedTask; }

    private async Task LoadCustomerSearchSitesAsync()
    {
        if (CustomerSelectedSearchCompany == null) { CustomerSearchSiteSuggestions.Clear(); CustomerSelectedSearchSite = null; IsCustomerSearchSiteEnabled = false; return; }
        try {
            var sites = await SiteService.LookupSitesForCompanyAsync(CustomerSelectedSearchCompany.CompanyId, "", CancellationToken.None);
            CustomerSearchSiteSuggestions.Clear();
            if (sites != null) foreach (var s in sites.OrderBy(x => x.SiteName)) CustomerSearchSiteSuggestions.Add(s);
        } catch { }
    }

    private async Task LoadBuyerSearchSitesAsync()
    {
        if (BuyerSelectedSearchCompany == null) { BuyerSearchSiteSuggestions.Clear(); BuyerSelectedSearchSite = null; IsBuyerSearchSiteEnabled = false; return; }
        try {
            var sites = await SiteService.LookupSitesForCompanyAsync(BuyerSelectedSearchCompany.CompanyId, "", CancellationToken.None);
            BuyerSearchSiteSuggestions.Clear();
            if (sites != null) foreach (var s in sites.OrderBy(x => x.SiteName)) BuyerSearchSiteSuggestions.Add(s);
        } catch { }
    }


    private async Task LoadSelectedCustomerSiteAddressAsync(CustomerDto? customer, CancellationToken ct = default)
    {
        if (customer?.CompanyId == null || customer.SiteId == null)
        {
            CustomerSiteAddressSummary = string.Empty;
            return;
        }

        try
        {
            var sites = await _app.SiteService.LookupSitesForCompanyAsync(
                customer.CompanyId.Value,
                term: string.Empty,
                ct);

            var site = sites?.FirstOrDefault(s => s.SiteId == customer.SiteId.Value);
            if (site == null)
            {
                CustomerSiteAddressSummary = string.Empty;
                return;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(site.AddressLine1)) parts.Add(site.AddressLine1);
            if (!string.IsNullOrWhiteSpace(site.AddressLine2)) parts.Add(site.AddressLine2);
            if (!string.IsNullOrWhiteSpace(site.Suburb))       parts.Add(site.Suburb);
            if (!string.IsNullOrWhiteSpace(site.City))         parts.Add(site.City);
            if (!string.IsNullOrWhiteSpace(site.PostalCode))   parts.Add(site.PostalCode);

            CustomerSiteAddressSummary = string.Join(", ", parts);
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load site address: {ex.Message}";
            CustomerSiteAddressSummary = string.Empty;
        }
    }

    private async Task LoadSelectedBuyerSiteAddressAsync(BuyerDto? buyer, CancellationToken ct = default)
    {
        if (buyer?.CompanyId == null || buyer.SiteId == null)
        {
            BuyerSiteAddressSummary = string.Empty;
            return;
        }

        try
        {
            var sites = await _app.SiteService.LookupSitesForCompanyAsync(
                buyer.CompanyId.Value,
                term: string.Empty,
                ct);

            var site = sites?.FirstOrDefault(s => s.SiteId == buyer.SiteId.Value);
            if (site == null)
            {
                BuyerSiteAddressSummary = string.Empty;
                return;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(site.AddressLine1)) parts.Add(site.AddressLine1);
            if (!string.IsNullOrWhiteSpace(site.AddressLine2)) parts.Add(site.AddressLine2);
            if (!string.IsNullOrWhiteSpace(site.Suburb))       parts.Add(site.Suburb);
            if (!string.IsNullOrWhiteSpace(site.City))         parts.Add(site.City);
            if (!string.IsNullOrWhiteSpace(site.PostalCode))   parts.Add(site.PostalCode);

            BuyerSiteAddressSummary = string.Join(", ", parts);
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load site address: {ex.Message}";
            BuyerSiteAddressSummary = string.Empty;
        }
    }

    private void PostUI(Action action)
    {
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
    }
}
