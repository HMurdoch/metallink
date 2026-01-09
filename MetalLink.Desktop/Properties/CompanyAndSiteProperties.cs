using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Customers;
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
        set
        {
            _searchCompanySuggestions = value;
            OnPropertyChanged();
        }
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

            // ✅ enable/disable Search Site dropdown
            IsSearchSiteEnabled = value != null;
            OnPropertyChanged(nameof(IsSearchSiteEnabled));

            // reset sites whenever company changes
            SearchSiteSuggestions.Clear();
            SelectedSearchSite = null;

            if (value != null)
                _ = LoadSitesForSelectedCompanyAsync();
        }
    }
}
