// MetalLink.Desktop/ViewModels/Properties/CustomerProperties.cs
using System.Collections.ObjectModel;
using System.Linq;
using MetalLink.Shared.Customers;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- Customer search text fields ---

    private string _searchCustomerIdText = string.Empty;
    private string _searchSiteIdText = string.Empty;
    private string _searchFirstNameText = string.Empty;
    private string _searchLastNameText = string.Empty;
    private string _searchCompanyNameText = string.Empty;
    private string _searchIdNumberText = string.Empty;
    private string _searchAddressLine1Text = string.Empty;
    private string _searchAddressLine2Text = string.Empty;
    private string _searchSuburbText = string.Empty;
    private string _searchCityText = string.Empty;
    private string _searchPostalCodeText = string.Empty;
    private string _searchPhoneNumberText = string.Empty;
    private string _searchMobileNumberText = string.Empty;
    private string _searchEmailText = string.Empty;
    private string _searchAccountNumberText = string.Empty;

    private ObservableCollection<CustomerDto> _customerSearchResults = new();

    private CustomerDto? _foundCustomer;

    public bool IsNewCustomerFullNameInvalid =>
        string.IsNullOrWhiteSpace(NewFirstName)
        && string.IsNullOrWhiteSpace(NewLastName)
        && string.IsNullOrWhiteSpace(NewCompanyName);

    public bool HasUnsavedNewCustomer =>
        !string.IsNullOrWhiteSpace(NewFirstName)
        || !string.IsNullOrWhiteSpace(NewLastName)
        || NewIsCompany
        || !string.IsNullOrWhiteSpace(NewCompanyName)
        || !string.IsNullOrWhiteSpace(NewIdNumber)
        || NewAccountNumber != null
        || !string.IsNullOrWhiteSpace(NewPriceCode)
        || !string.IsNullOrWhiteSpace(NewAddressLine1)
        || !string.IsNullOrWhiteSpace(NewAddressLine2)
        || !string.IsNullOrWhiteSpace(NewSuburb)
        || !string.IsNullOrWhiteSpace(NewCity)
        || !string.IsNullOrWhiteSpace(NewPostalCode)
        || !string.IsNullOrWhiteSpace(NewPhoneNumber)
        || !string.IsNullOrWhiteSpace(NewMobileNumber)
        || !string.IsNullOrWhiteSpace(NewEmail);

    // --- Customer search properties ---

    public string SearchCustomerIdText
    {
        get => _searchCustomerIdText;
        set { _searchCustomerIdText = value; OnPropertyChanged(); }
    }

    public string SearchSiteIdText
    {
        get => _searchSiteIdText;
        set { _searchSiteIdText = value; OnPropertyChanged(); }
    }

    public string SearchFirstNameText
    {
        get => _searchFirstNameText;
        set { _searchFirstNameText = value; OnPropertyChanged(); }
    }

    public string SearchLastNameText
    {
        get => _searchLastNameText;
        set { _searchLastNameText = value; OnPropertyChanged(); }
    }

    public string SearchCompanyNameText
    {
        get => _searchCompanyNameText;
        set { _searchCompanyNameText = value; OnPropertyChanged(); }
    }

    public string SearchIdNumberText
    {
        get => _searchIdNumberText;
        set { _searchIdNumberText = value; OnPropertyChanged(); }
    }

    public string SearchAddressLine1Text
    {
        get => _searchAddressLine1Text;
        set { _searchAddressLine1Text = value; OnPropertyChanged(); }
    }

    public string SearchAddressLine2Text
    {
        get => _searchAddressLine2Text;
        set { _searchAddressLine2Text = value; OnPropertyChanged(); }
    }

    public string SearchSuburbText
    {
        get => _searchSuburbText;
        set { _searchSuburbText = value; OnPropertyChanged(); }
    }

    public string SearchCityText
    {
        get => _searchCityText;
        set { _searchCityText = value; OnPropertyChanged(); }
    }

    public string SearchPostalCodeText
    {
        get => _searchPostalCodeText;
        set { _searchPostalCodeText = value; OnPropertyChanged(); }
    }

    public string SearchPhoneNumberText
    {
        get => _searchPhoneNumberText;
        set { _searchPhoneNumberText = value; OnPropertyChanged(); }
    }

    public string SearchMobileNumberText
    {
        get => _searchMobileNumberText;
        set { _searchMobileNumberText = value; OnPropertyChanged(); }
    }

    public string SearchEmailText
    {
        get => _searchEmailText;
        set { _searchEmailText = value; OnPropertyChanged(); }
    }

    public string SearchAccountNumberText
    {
        get => _searchAccountNumberText;
        set { _searchAccountNumberText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CustomerDto> CustomerSearchResults
    {
        get => _customerSearchResults;
        set { _customerSearchResults = value; OnPropertyChanged(); }
    }

    private int _totalCustomersInDb;
    public int TotalCustomersInDb
    {
        get => _totalCustomersInDb;
        set { _totalCustomersInDb = value; OnPropertyChanged(); }
    }

    private int _totalTicketsInDb;
    public int TotalTicketsInDb
    {
        get => _totalTicketsInDb;
        set { _totalTicketsInDb = value; OnPropertyChanged(); }
    }
    // --- Loaded / selected customer driving the details panel ---

    public CustomerDto? FoundCustomer
    {
        get => _foundCustomer;
        set
        {
            _foundCustomer = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FoundCustomerSummary));
            OnPropertyChanged(nameof(SelectedCustomerIdDisplay));
            OnPropertyChanged(nameof(SelectedFirstName));
            OnPropertyChanged(nameof(SelectedLastName));
            OnPropertyChanged(nameof(SelectedCompanyName));
            OnPropertyChanged(nameof(SelectedSiteName));
            OnPropertyChanged(nameof(SelectedAccountNumberFormatted));
            OnPropertyChanged(nameof(SelectedIdNumber));
            OnPropertyChanged(nameof(SelectedAccountNumber));
            OnPropertyChanged(nameof(SelectedPriceCode));
            OnPropertyChanged(nameof(SelectedPhoneNumber));
            OnPropertyChanged(nameof(SelectedMobileNumber));
            OnPropertyChanged(nameof(SelectedEmail));
            OnPropertyChanged(nameof(SelectedTaxable));

            // Load site address summary (from CAS/Site) for customer details panel
            _ = LoadSelectedCustomerSiteAddressAsync(_foundCustomer);

            // Load customer images if available
            _ = LoadSelectedCustomerImagesAsync(_foundCustomer);

            // Populate Create/Edit form when selecting from results (no code-behind)
            if (_foundCustomer != null)
            {
                IsEditMode = true;
                EditingCustomerId = _foundCustomer.CustomerId;

                NewFirstName = _foundCustomer.FirstName ?? string.Empty;
                NewLastName = _foundCustomer.LastName ?? string.Empty;
                NewIdNumber = _foundCustomer.IdNumber;
                NewEmail = _foundCustomer.Email ?? string.Empty;
                NewPhoneNumber = _foundCustomer.PhoneNumber ?? string.Empty;
                NewMobileNumber = _foundCustomer.MobileNumber ?? string.Empty;
                NewTaxable = _foundCustomer.Taxable;
                NewAccountNumber = _foundCustomer.AccountNumber;

                // Set company/site BEFORE setting NewIsCompany (so selections aren't cleared)
                if (_foundCustomer.IsCompany && _foundCustomer.CompanyId.HasValue)
                {
                    var company = _allCompanies.FirstOrDefault(c => c.CompanyId == _foundCustomer.CompanyId.Value);
                    if (company != null && !string.IsNullOrWhiteSpace(company.CompanyName))
                    {
                        var letter = char.ToUpperInvariant(company.CompanyName[0]).ToString();
                        SelectedNewCompanyLetter = CompanyLetterFilters.Contains(letter) ? letter : "ALL";
                    }

                    // Set pending site selection BEFORE selecting company (company selection clears sites and loads async)
                    _pendingSelectSiteId = _foundCustomer.SiteId;

                    SelectedNewCompany = NewCompanySuggestions.FirstOrDefault(c => c.CompanyId == _foundCustomer.CompanyId.Value);
                }

                // Set NewIsCompany AFTER company/site selections so they don't get cleared
                NewIsCompany = _foundCustomer.IsCompany;

                if (!string.IsNullOrWhiteSpace(_foundCustomer.PriceCode))
                    SelectedPriceCodeChar = PriceCodeOptions.FirstOrDefault(p => p.Code == _foundCustomer.PriceCode);

                OnPropertyChanged(nameof(CanCreateCustomer));
                OnPropertyChanged(nameof(CanUpdateCustomer));
                (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
            else
            {
                IsEditMode = false;
                EditingCustomerId = null;
                OnPropertyChanged(nameof(CanUpdateCustomer));
                (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public string FoundCustomerSummary =>
        FoundCustomer == null
            ? "No customer loaded."
            : $"ID: {FoundCustomer.CustomerId:D8}, Name: {FoundCustomer.FirstName} {FoundCustomer.LastName}, Account: {FoundCustomer.AccountNumber}";

    // 8-digit, zero-padded ID
    public string SelectedCustomerIdDisplay =>
        FoundCustomer == null ? string.Empty : FoundCustomer.CustomerId.ToString("D8");

    public string SelectedFirstName      => FoundCustomer?.FirstName ?? "";
    public string SelectedLastName       => FoundCustomer?.LastName ?? "";
    public string SelectedCompanyName    => FoundCustomer?.CompanyName ?? string.Empty;
    public string SelectedSiteName       => FoundCustomer?.SiteName ?? string.Empty;
    public string SelectedAccountNumberFormatted    => FoundCustomer?.AccountNumberFormatted ?? string.Empty;
    public string SelectedIdNumber       => FoundCustomer?.IdNumber ?? string.Empty;
    public long? SelectedAccountNumber  => FoundCustomer?.AccountNumber;
    public string SelectedPriceCode      => FoundCustomer?.PriceCode ?? string.Empty;
    public string SelectedPhoneNumber    => FoundCustomer?.PhoneNumber ?? string.Empty;
    public string SelectedMobileNumber   => FoundCustomer?.MobileNumber ?? string.Empty;
    public string SelectedEmail          => FoundCustomer?.Email ?? string.Empty;
    public bool   SelectedTaxable        => FoundCustomer?.Taxable ?? false;


    // --- New customer form properties ---


    private string _newFirstName = string.Empty;
    public string NewFirstName
    {
        get => _newFirstName;
        set
        {
            _newFirstName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewCustomerFullNameInvalid));
            OnPropertyChanged(nameof(IsNewBuyerFullNameInvalid));
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));

            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string _newLastName = string.Empty;
    public string NewLastName
    {
        get => _newLastName;
        set
        {
            _newLastName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNewCustomerFullNameInvalid));
            OnPropertyChanged(nameof(IsNewBuyerFullNameInvalid));
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));

            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private bool _newIsCompany;
    public bool NewIsCompany
    {
        get => _newIsCompany;
        set
        {
            _newIsCompany = value;
            if (!NewIsCompany)
            {
                SelectedNewCompany = null;
                SelectedNewSite = null;
                NewSiteSuggestions.Clear();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private string? _newCompanyName;
    public string? NewCompanyName
    {
        get => _newCompanyName;
        set
        {
            _newCompanyName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
    }

    private string? _newIdNumber;
    public string? NewIdNumber
    {
        get => _newIdNumber;
        set
        {
            _newIdNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));

            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private long? _newAccountNumber;

    public long? NewAccountNumber
    {
        get => _newAccountNumber;
        private set
        {
            _newAccountNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NewAccountNumberDisplay));
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));

            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    public string NewAccountNumberDisplay =>
        NewAccountNumber.HasValue ? NewAccountNumber.Value.ToString("D8") : string.Empty;

    private string? _newPriceCode;
    public string? NewPriceCode
    {
        get => _newPriceCode;
        set
        {
            _newPriceCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    private string? _newPhoneNumber;
    public string? NewPhoneNumber
    {
        get => _newPhoneNumber;
        set
        {
            _newPhoneNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));

            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string? _newMobileNumber;
    public string? NewMobileNumber
    {
        get => _newMobileNumber;
        set
        {
            _newMobileNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));

            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private string? _newEmail;
    public string? NewEmail
    {
        get => _newEmail;
        set
        {
            _newEmail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedNewCustomer));
            OnPropertyChanged(nameof(HasUnsavedChanges));

            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    private bool _newTaxable = true;
    public bool NewTaxable
    {
        get => _newTaxable;
        set
        {
            _newTaxable = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanCreateBuyer));
            (UpdateBuyerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }
    
    public sealed record PriceCodeOption(string Code, string Label);

    public ObservableCollection<PriceCodeOption> PriceCodeOptions { get; } =
    [
        new("A", "Price A"),
        new("B", "Price B"),
        new("C", "Price C"),
    ];

    private void SyncPriceCodeDropdownFromNewPriceCode()
    {
        var code = (NewPriceCode ?? "").Trim();
        SelectedPriceCodeChar = PriceCodeOptions.FirstOrDefault(x => x.Code == code);
    }

    private PriceCodeOption? _selectedPriceCodeChar;

    public PriceCodeOption? SelectedPriceCodeChar
    {
        get => _selectedPriceCodeChar;
        set
        {
            if (_selectedPriceCodeChar == value) return;
            _selectedPriceCodeChar = value;
            OnPropertyChanged();

            NewPriceCode = _selectedPriceCodeChar?.Code;

            // if you have CanCreate/CanUpdate checks depending on it, notify here too
            (CreateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    // For search panel
    private PriceCodeOption? _searchPriceCode;

    public PriceCodeOption? SearchPriceCode
    {
        get => _searchPriceCode;
        set
        {
            if (_searchPriceCode == value) return;
            _searchPriceCode = value;
            OnPropertyChanged();

            // IMPORTANT: search depends on this
            (SearchCustomersCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)
                ?.NotifyCanExecuteChanged();
        }
    }

    // --- Customer Image Properties ---
    
    private Avalonia.Media.Imaging.Bitmap? _idCardImage;
    public Avalonia.Media.Imaging.Bitmap? IdCardImage
    {
        get => _idCardImage;
        set
        {
            _idCardImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _driverLicenseImage;
    public Avalonia.Media.Imaging.Bitmap? DriverLicenseImage
    {
        get => _driverLicenseImage;
        set
        {
            _driverLicenseImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _photoImage;
    public Avalonia.Media.Imaging.Bitmap? PhotoImage
    {
        get => _photoImage;
        set
        {
            _photoImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _signatureImage;
    public Avalonia.Media.Imaging.Bitmap? SignatureImage
    {
        get => _signatureImage;
        set
        {
            _signatureImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _fingerprintImage;
    public Avalonia.Media.Imaging.Bitmap? FingerprintImage
    {
        get => _fingerprintImage;
        set
        {
            _fingerprintImage = value;
            OnPropertyChanged();
        }
    }

    // Selected customer image display properties
    private Avalonia.Media.Imaging.Bitmap? _selectedIdCardImage;
    public Avalonia.Media.Imaging.Bitmap? SelectedIdCardImage
    {
        get => _selectedIdCardImage;
        set
        {
            _selectedIdCardImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _selectedDriverLicenseImage;
    public Avalonia.Media.Imaging.Bitmap? SelectedDriverLicenseImage
    {
        get => _selectedDriverLicenseImage;
        set
        {
            _selectedDriverLicenseImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _selectedPhotoImage;
    public Avalonia.Media.Imaging.Bitmap? SelectedPhotoImage
    {
        get => _selectedPhotoImage;
        set
        {
            _selectedPhotoImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _selectedSignatureImage;
    public Avalonia.Media.Imaging.Bitmap? SelectedSignatureImage
    {
        get => _selectedSignatureImage;
        set
        {
            _selectedSignatureImage = value;
            OnPropertyChanged();
        }
    }

    private Avalonia.Media.Imaging.Bitmap? _selectedFingerprintImage;
    public Avalonia.Media.Imaging.Bitmap? SelectedFingerprintImage
    {
        get => _selectedFingerprintImage;
        set
        {
            _selectedFingerprintImage = value;
            OnPropertyChanged();
        }
    }
}
