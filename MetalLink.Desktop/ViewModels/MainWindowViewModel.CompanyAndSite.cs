using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using MetalLink.Desktop.Services;
using Avalonia.Threading;
using System.Collections.Generic;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
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
            ClearCompanyFormCommand = new AsyncRelayCommand(ct => ClearCompanyFormAsync(ct));
            UpdateCompany2Command = new AsyncRelayCommand(ct => UpdateCompanyAsync(ct), () => CanUpdateCompany);
            ClearSiteFormCommand = new RelayCommand(ClearSiteForm);
    }

    // =====================================================
    // IMPLEMENTATION (TODO hooks – wire to your services)
    // =====================================================

    private async Task SearchCompaniesAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            StatusMessage = "Searching companies...";

            // Ensure lookup cache is loaded (CompanyLetterFilters triggers lazy load)
            _ = CompanyLetterFilters;

            // If still loading, you can optionally wait a tiny bit, but usually it's ready quickly.
            // (If you want to be strict, add a flag in Lookup.cs to expose loading state.)

            // Start from cached master list
            var query = _allCompanies.AsEnumerable();

            // Apply letter filter (ALL = no filter)
            var letter = (SelectedCompanyLetter ?? "ALL").Trim();

            if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(letter))
            {
                var ch = char.ToUpperInvariant(letter[0]);

                query = query.Where(c =>
                    !string.IsNullOrWhiteSpace(c.CompanyName) &&
                    char.ToUpperInvariant(c.CompanyName![0]) == ch);
            }

            // Populate results grid
            CompanyResults.Clear();
            foreach (var c in query.OrderBy(c => c.CompanyName))
                CompanyResults.Add(c);

            // Optional: auto-select first row (or set null)
            SelectedCompany = CompanyResults.FirstOrDefault();

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

    private void ClearSiteForm()
    {
        EditingSiteId = null;
        SelectedSite = null;

        SiteName = "";
        SiteCode = "";
        AddressLine1 = "";
        AddressLine2 = "";
        Suburb = "";
        City = "";
        PostalCode = "";

        // defaults
        SetDefaultProvinceAndCountryForSite();

        OnPropertyChanged(nameof(SiteSaveButtonText));
        (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
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
                    CountryId = NewCountry?.CountryId ?? SelectedCountry?.CountryId ?? 1
                }, ct);

            if (site == null)
                throw new Exception("CreateSite returned null.");

            // Clear UI
            CompanyEditName = string.Empty;
            CompanyVatNumber = string.Empty;
            CompanyFormInitialSiteName = string.Empty;

            // Refresh your caches/results
            // If you already have a lookup cache loader, call it.
            // Otherwise, simplest: re-run company search / reload letters.
            StatusMessage = $"[STATUS] Created company {company.CompanyName} and site {site.SiteName}.";

            // Optional: reload companies list if your UI needs it
            // await SearchCompaniesAsync(); // if you have it

            // Convert to lookup dto if your API returns CompanyDto
            var lookup = new CompanyLookupDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.CompanyName,
                VatNumber = company.VatNumber,
                IsActive = company.IsActive
            };

            // Run UI updates on UI thread (Avalonia-safe)
            Dispatcher.UIThread.Post(() =>
            {
                AddCompanyToCachesAndSelect(lookup);
            }, DispatcherPriority.Background);

            var createdId = lookup.CompanyId;

            Dispatcher.UIThread.Post(() =>
            {
                AddCompanyToCachesAndSelect(lookup);

                // Ensure the grid selection is the created company (in case anything refreshed)
                SelectedCompany = CompanyResults.FirstOrDefault(c => c.CompanyId == createdId)
                                ?? SelectedCompany;
            }, DispatcherPriority.Background);

            // optional: load sites for the selected company (only if endpoint exists)
            // Populate results grid
            var keepId = SelectedCompany?.CompanyId; // preserve current selection if possible

            //CompanyResults.Clear();
            foreach (var c in CompanyResults.OrderBy(c => c.CompanyName))
                CompanyResults.Add(c);

            // Re-select previous (or keep current), otherwise pick first
            if (keepId.HasValue)
                SelectedCompany = CompanyResults.FirstOrDefault(x => x.CompanyId == keepId.Value) 
                                ?? CompanyResults.FirstOrDefault();
            else
                SelectedCompany = CompanyResults.FirstOrDefault();
            _ = LoadSitesForSelectedCompanyResultsAsync(ct);
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
                CompanyId = SelectedCompany.CompanyId,                 // harmless
                CompanyName = CompanyEditName.Trim(),
                VatNumber = string.IsNullOrWhiteSpace(CompanyVatNumber) ? null : CompanyVatNumber.Trim(),
                IsActive = true
            };

            await _app.CompanyAndSiteService.UpdateCompanyAsync(SelectedCompany.CompanyId, dto, ct);

            var idx = CompanyResults.IndexOf(SelectedCompany);
            if (idx >= 0)
            {
                var updatedLookup = new CompanyLookupDto
                {
                    CompanyId = SelectedCompany.CompanyId,
                    CompanyName = dto.CompanyName,
                    VatNumber = dto.VatNumber,
                    IsActive = true
                };

                CompanyResults[idx] = updatedLookup;
                SelectedCompany = updatedLookup; // keep selection + triggers site load
            }

            // ✅ Keep lookup cache in sync if you have it
            var cache = _allCompanies?.FirstOrDefault(x => x.CompanyId == dto.CompanyId);
            if (cache != null)
            {
                cache.CompanyName = dto.CompanyName;
                cache.VatNumber = dto.VatNumber;
            }

            // re-render grid
            OnPropertyChanged(nameof(CompanyResults));
            OnPropertyChanged(nameof(SelectedCompany));

            StatusMessage = $"[STATUS] Company updated: {dto.CompanyName}";

            // refresh sites list (optional but nice)
            await LoadSitesForSelectedCompanyResultsAsync(ct);
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
        EditingCompanyId = null;

        CompanyFormName = "";
        CompanyFormVatNumber = "";

        CompanyEditName = string.Empty;
        CompanyVatNumber = string.Empty;
        CompanyFormInitialSiteName = string.Empty;

        StatusMessage = "[STATUS] Company form cleared.";
        return Task.CompletedTask;
    }

    private async Task<bool> ConfirmAsync(string message)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return false;

        var dlg = new MetalLink.Desktop.Views.ConfirmDialog(message);
        return await dlg.ShowDialog<bool>(owner);
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

        if (site.ProvinceId.HasValue)
            SelectedProvince = Provinces.FirstOrDefault(p => p.ProvinceId == site.ProvinceId.Value);

        if (site.CountryId.HasValue)
            SelectedCountry = Countries.FirstOrDefault(c => c.CountryId == site.CountryId.Value);

        // fallback defaults if null
        if (SelectedProvince == null || SelectedCountry == null)
            SetDefaultProvinceAndCountryForSite();

        OnPropertyChanged(nameof(SiteSaveButtonText));
        (CreateOrUpdateSiteCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
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
                        CompanyId = SelectedCompany.CompanyId,
                        SiteName = SiteName.Trim(),
                        SiteCode = string.IsNullOrWhiteSpace(SiteCode) ? null : SiteCode.Trim(),
                        AddressLine1 = string.IsNullOrWhiteSpace(AddressLine1) ? null : AddressLine1.Trim(),
                        AddressLine2 = string.IsNullOrWhiteSpace(AddressLine2) ? null : AddressLine2.Trim(),
                        Suburb = string.IsNullOrWhiteSpace(Suburb) ? null : Suburb.Trim(),
                        City = string.IsNullOrWhiteSpace(City) ? null : City.Trim(),
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

            SelectedSearchCompany = SearchCompanySuggestions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Company refresh failed: {ex.Message}";
        }
    }

    private async Task LoadSitesForSelectedCompanyResultsAsync(CancellationToken ct = default)
    {
        if (SelectedCompany == null)
        {
            SiteResults.Clear();
            SelectedSite = null;
            return;
        }

        try
        {
            StatusMessage = "Loading sites...";

            var items = await _app.SiteService.LookupSitesForCompanyAsync(
                SelectedCompany.CompanyId,
                term: string.Empty,
                ct) ?? new List<SiteLookupDto>();

            SiteResults.Clear();
            foreach (var s in items.OrderBy(x => x.SiteName))
                SiteResults.Add(s);

            StatusMessage = $"Loaded {SiteResults.Count} site(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load sites failed: {ex.Message}";
            SiteResults.Clear();
        }
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
                    CompanyId = SelectedCompany.CompanyId,
                    SiteName = NewSiteCreateName.Trim(),
                    SiteCode = string.IsNullOrWhiteSpace(NewSiteCreateCode) ? null : NewSiteCreateCode.Trim(),
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


    private readonly ObservableCollection<CompanyLookupDto> _allCompanies = new();

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

    private bool _companyLettersLoaded;
    private bool _restoringSearchCompanySelection;
    private void ApplyCompanyLetterFilter()
    {
        if (!_companyLettersLoaded) return;

        var selectedId = SelectedSearchCompany?.CompanyId;
        var letter = (SelectedCompanyLetter ?? "ALL").Trim();

        SearchCompanySuggestions.Clear();

        IEnumerable<CompanyLookupDto> query = _allCompanies;

        if (!letter.Equals("ALL", StringComparison.OrdinalIgnoreCase) && letter.Length > 0)
        {
            var ch = char.ToUpperInvariant(letter[0]);
            query = query.Where(c =>
                !string.IsNullOrWhiteSpace(c.CompanyName) &&
                char.ToUpperInvariant(c.CompanyName![0]) == ch);
        }

        foreach (var c in query.OrderBy(c => c.CompanyName))
            SearchCompanySuggestions.Add(c);

        if (selectedId.HasValue)
        {
            var match = SearchCompanySuggestions.FirstOrDefault(x => x.CompanyId == selectedId.Value);

            _restoringSearchCompanySelection = true;
            SelectedSearchCompany = match;
            _restoringSearchCompanySelection = false;
        }
    }


    private void PostUI(Action action)
    {
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
    }
}
