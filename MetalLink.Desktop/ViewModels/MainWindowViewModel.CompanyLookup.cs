using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using MetalLink.Shared.Companies;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Master list for letter filtering (populated lazily)
    private readonly ObservableCollection<CompanyLookupDto> _allCompanies = new();
    private readonly ObservableCollection<string> _companyLetterFilters = new();

    private bool _companyLettersLoaded;
    private bool _companyLettersLoading;

    // Prevent changing ItemsSource mid-selection (avoids Avalonia index exceptions)
    private bool _syncingCompanyLetter;
    private bool _suppressLetterApply;

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

    private string? _selectedCompanyLetter = "ALL";
    public string? SelectedCompanyLetter
    {
        get => _selectedCompanyLetter;
        set
        {
            if (_selectedCompanyLetter == value) return;
            _selectedCompanyLetter = value;
            OnPropertyChanged();

            // ✅ user changed letter -> clear selection and sites
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

            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;

            if (value != null)
                _ = LoadSitesForSelectedCompanyAsync();
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
