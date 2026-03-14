using System.Collections.ObjectModel;
using System.Linq;
using MetalLink.Shared.Prices;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Buyers;
using Avalonia.Media.Imaging;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // --- UI Toggles for ALL SYSTEMS (Independent) ---
    private bool _customerIsSearchCriteriaExpanded = true;
    public bool CustomerIsSearchCriteriaExpanded { get => _customerIsSearchCriteriaExpanded; set { _customerIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _customerIsSearchResultsExpanded;
    public bool CustomerIsSearchResultsExpanded { get => _customerIsSearchResultsExpanded; set { _customerIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    
    private bool _customerIsDetailsExpanded;
    public bool CustomerIsDetailsExpanded { get => _customerIsDetailsExpanded; set { _customerIsDetailsExpanded = value; OnPropertyChanged(); } }
    
    private bool _customerIsCreateEditExpanded = true;
    public bool CustomerIsCreateEditExpanded { get => _customerIsCreateEditExpanded; set { _customerIsCreateEditExpanded = value; OnPropertyChanged(); } }
    
    private bool _customerIsPanelExpanded = true;
    public bool CustomerIsPanelExpanded { get => _customerIsPanelExpanded; set { _customerIsPanelExpanded = value; OnPropertyChanged(); } }

    private bool _buyerIsSearchCriteriaExpanded = true;
    public bool BuyerIsSearchCriteriaExpanded { get => _buyerIsSearchCriteriaExpanded; set { _buyerIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _buyerIsSearchResultsExpanded;
    public bool BuyerIsSearchResultsExpanded { get => _buyerIsSearchResultsExpanded; set { _buyerIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    
    private bool _buyerIsDetailsExpanded;
    public bool BuyerIsDetailsExpanded { get => _buyerIsDetailsExpanded; set { _buyerIsDetailsExpanded = value; OnPropertyChanged(); } }
    
    private bool _buyerIsCreateEditExpanded = true;
    public bool BuyerIsCreateEditExpanded { get => _buyerIsCreateEditExpanded; set { _buyerIsCreateEditExpanded = value; OnPropertyChanged(); } }
    
    private bool _buyerIsPanelExpanded = true;
    public bool BuyerIsPanelExpanded { get => _buyerIsPanelExpanded; set { _buyerIsPanelExpanded = value; OnPropertyChanged(); } }

    private bool _companyIsSearchCriteriaExpanded = true;
    public bool CompanyIsSearchCriteriaExpanded { get => _companyIsSearchCriteriaExpanded; set { _companyIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _companyIsSearchResultsExpanded;
    public bool CompanyIsSearchResultsExpanded { get => _companyIsSearchResultsExpanded; set { _companyIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    
    private bool _companyIsDetailsExpanded;
    public bool CompanyIsDetailsExpanded { get => _companyIsDetailsExpanded; set { _companyIsDetailsExpanded = value; OnPropertyChanged(); } }
    
    private bool _companyIsCreateEditExpanded = true;
    public bool CompanyIsCreateEditExpanded { get => _companyIsCreateEditExpanded; set { _companyIsCreateEditExpanded = value; OnPropertyChanged(); } }
    
    private bool _companyIsPanelExpanded = true;
    public bool CompanyIsPanelExpanded { get => _companyIsPanelExpanded; set { _companyIsPanelExpanded = value; OnPropertyChanged(); } }

    private bool _productsIsSearchCriteriaExpanded = true;
    public bool ProductsIsSearchCriteriaExpanded { get => _productsIsSearchCriteriaExpanded; set { _productsIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _productsIsSearchResultsExpanded;
    public bool ProductsIsSearchResultsExpanded { get => _productsIsSearchResultsExpanded; set { _productsIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    
    private bool _productsIsDetailsExpanded;
    public bool ProductsIsDetailsExpanded { get => _productsIsDetailsExpanded; set { _productsIsDetailsExpanded = value; OnPropertyChanged(); } }
    
    private bool _productsIsCreateEditExpanded = true;
    public bool ProductsIsCreateEditExpanded { get => _productsIsCreateEditExpanded; set { _productsIsCreateEditExpanded = value; OnPropertyChanged(); } }
    
    private bool _productsIsPanelExpanded = true;
    public bool ProductsIsPanelExpanded { get => _productsIsPanelExpanded; set { _productsIsPanelExpanded = value; OnPropertyChanged(); } }

    private bool _dashboardIsStatsExpanded = true;
    public bool DashboardIsStatsExpanded { get => _dashboardIsStatsExpanded; set { _dashboardIsStatsExpanded = value; OnPropertyChanged(); } }
    
    private bool _dashboardIsChartsExpanded = true;
    public bool DashboardIsChartsExpanded { get => _dashboardIsChartsExpanded; set { _dashboardIsChartsExpanded = value; OnPropertyChanged(); } }

    private bool _documentsIsSearchCriteriaExpanded = true;
    public bool DocumentsIsSearchCriteriaExpanded { get => _documentsIsSearchCriteriaExpanded; set { _documentsIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _documentsIsSearchResultsExpanded;
    public bool DocumentsIsSearchResultsExpanded { get => _documentsIsSearchResultsExpanded; set { _documentsIsSearchResultsExpanded = value; OnPropertyChanged(); } }

    private bool _cameraIsSettingsExpanded = true;
    public bool CameraIsSettingsExpanded { get => _cameraIsSettingsExpanded; set { _cameraIsSettingsExpanded = value; OnPropertyChanged(); } }
    
    private bool _cameraIsPreviewExpanded = true;
    public bool CameraIsPreviewExpanded { get => _cameraIsPreviewExpanded; set { _cameraIsPreviewExpanded = value; OnPropertyChanged(); } }

    // --- Search / Selection Results (Core State) ---
    private CustomerDto? _foundCustomer;
    public CustomerDto? FoundCustomer 
    { 
        get => _foundCustomer; 
        set 
        { 
            _foundCustomer = value; 
            OnPropertyChanged(); 
            NotifySelectedEntityProperties(); 
            if (value != null) 
            {
                PopulateCustomerCreateEdit(value);
                _ = LoadSelectedCustomerImagesAsync(value);
            }
        } 
    }

    private BuyerDto? _foundBuyer;
    public BuyerDto? FoundBuyer 
    { 
        get => _foundBuyer; 
        set 
        { 
            _foundBuyer = value; 
            OnPropertyChanged(); 
            NotifySelectedEntityProperties(); 
            if (value != null) 
            {
                PopulateBuyerCreateEdit(value);
                _ = LoadSelectedBuyerImagesAsync(value);
            }
        } 
    }

    private async void PopulateCustomerCreateEdit(CustomerDto customer)
    {
        IsEditMode = true;
        EditingCustomerId = customer.CustomerId;
        NewFirstName = customer.FirstName ?? string.Empty;
        NewLastName = customer.LastName ?? string.Empty;
        NewCompanyName = customer.CompanyName;
        NewIdNumber = customer.IdNumber;
        NewEmail = customer.Email;
        NewPhoneNumber = customer.PhoneNumber;
        NewMobileNumber = customer.MobileNumber;
        NewTaxable = customer.Taxable;
        NewAccountNumber = customer.AccountNumber;
        NewIsCompany = customer.IsCompany;
        
        // Match price list
        if (customer.ProductPriceListId.HasValue)
            SelectedNewProductPriceList = CustomerPriceLists.FirstOrDefault(x => x.ProductPriceListId == customer.ProductPriceListId.Value);
            
        // Match company/site for existing customer
        if (customer.CompanyId.HasValue)
        {
            // Set letter to ALL or the first letter of the company name to ensure suggestions are loaded
            SelectedNewCompanyLetter = "ALL";
            ApplyNewCompanyLetterFilter();
            
            SelectedNewCompany = NewCompanySuggestions.FirstOrDefault(x => x.CompanyId == customer.CompanyId.Value);
            if (SelectedNewCompany != null)
            {
                await LoadNewSitesAndSelectAsync(customer.SiteId);
            }
        }
    }

    private async void PopulateBuyerCreateEdit(BuyerDto buyer)
    {
        IsEditMode = true;
        EditingBuyerId = buyer.BuyerId;
        NewFirstName = buyer.FirstName ?? string.Empty;
        NewLastName = buyer.LastName ?? string.Empty;
        NewCompanyName = buyer.CompanyName;
        NewIdNumber = buyer.IdNumber;
        NewEmail = buyer.Email;
        NewPhoneNumber = buyer.PhoneNumber;
        NewMobileNumber = buyer.MobileNumber;
        NewTaxable = buyer.Taxable;
        NewAccountNumber = buyer.AccountNumber;
        NewIsCompany = buyer.IsCompany;

        // Match price list
        if (buyer.ProductPriceListId.HasValue)
            SelectedNewProductPriceList = CustomerPriceLists.FirstOrDefault(x => x.ProductPriceListId == buyer.ProductPriceListId.Value);

        // Match company/site
        if (buyer.CompanyId.HasValue)
        {
            SelectedNewCompanyLetter = "ALL";
            ApplyNewCompanyLetterFilter();
            
            SelectedNewCompany = NewCompanySuggestions.FirstOrDefault(x => x.CompanyId == buyer.CompanyId.Value);
            if (SelectedNewCompany != null)
            {
                await LoadNewSitesAndSelectAsync(buyer.SiteId);
            }
        }
    }

    private bool _isEditMode;
    public bool IsEditMode { get => _isEditMode; set { _isEditMode = value; OnPropertyChanged(); } }

    private int? _editingCustomerId;
    public int? EditingCustomerId { get => _editingCustomerId; set { _editingCustomerId = value; OnPropertyChanged(); } }

    private int? _editingBuyerId;
    public int? EditingBuyerId { get => _editingBuyerId; set { _editingBuyerId = value; OnPropertyChanged(); } }

    // --- Form Fields ---
    private string _newFirstName = string.Empty;
    public string NewFirstName { get => _newFirstName; set { _newFirstName = value; OnPropertyChanged(); OnPropertyChanged("IsNewCustomerFullNameInvalid"); OnPropertyChanged("IsNewBuyerFullNameInvalid"); OnPropertyChanged("CanCreateCustomer"); OnPropertyChanged("CanCreateBuyer"); OnPropertyChanged("CanUpdateCustomer"); OnPropertyChanged("CanUpdateBuyer"); OnPropertyChanged("HasUnsavedNewCustomer"); OnPropertyChanged("HasUnsavedChanges"); } }

    private string _newLastName = string.Empty;
    public string NewLastName { get => _newLastName; set { _newLastName = value; OnPropertyChanged(); OnPropertyChanged("IsNewCustomerFullNameInvalid"); OnPropertyChanged("IsNewBuyerFullNameInvalid"); OnPropertyChanged("CanCreateCustomer"); OnPropertyChanged("CanCreateBuyer"); OnPropertyChanged("CanUpdateCustomer"); OnPropertyChanged("CanUpdateBuyer"); OnPropertyChanged("HasUnsavedNewCustomer"); OnPropertyChanged("HasUnsavedChanges"); } }

    private string? _newCompanyName;
    public string? NewCompanyName { get => _newCompanyName; set { _newCompanyName = value; OnPropertyChanged(); OnPropertyChanged("IsNewCustomerFullNameInvalid"); OnPropertyChanged("IsNewBuyerFullNameInvalid"); OnPropertyChanged("CanCreateCustomer"); OnPropertyChanged("CanCreateBuyer"); OnPropertyChanged("CanUpdateCustomer"); OnPropertyChanged("CanUpdateBuyer"); OnPropertyChanged("HasUnsavedNewCustomer"); OnPropertyChanged("HasUnsavedChanges"); } }

    private string? _newIdNumber;
    public string? NewIdNumber { get => _newIdNumber; set { _newIdNumber = value; OnPropertyChanged(); } }

    private string? _newEmail;
    public string? NewEmail { get => _newEmail; set { _newEmail = value; OnPropertyChanged(); } }

    private string? _newPhoneNumber;
    public string? NewPhoneNumber { get => _newPhoneNumber; set { _newPhoneNumber = value; OnPropertyChanged(); } }

    private string? _newMobileNumber;
    public string? NewMobileNumber { get => _newMobileNumber; set { _newMobileNumber = value; OnPropertyChanged(); } }

    private bool _newTaxable = true;
    public bool NewTaxable { get => _newTaxable; set { _newTaxable = value; OnPropertyChanged(); } }

    private long? _newAccountNumber;
    public long? NewAccountNumber { get => _newAccountNumber; set { _newAccountNumber = value; OnPropertyChanged(); OnPropertyChanged("NewAccountNumberDisplay"); } }

    public string NewAccountNumberDisplay => NewAccountNumber.HasValue ? NewAccountNumber.Value.ToString("D8") : string.Empty;

    private bool _newIsCompany;
    public bool NewIsCompany 
    { 
        get => EnforceBuyerCompany ? true : _newIsCompany; 
        set { _newIsCompany = value; OnPropertyChanged(); } 
    }

    private ProductPriceListDto? _selectedNewProductPriceList;
    public ProductPriceListDto? SelectedNewProductPriceList { get => _selectedNewProductPriceList; set { _selectedNewProductPriceList = value; OnPropertyChanged(); } }

    // --- Computed Selection Properties for Details View ---
    public string SelectedFirstName => FoundCustomer?.FirstName ?? FoundBuyer?.FirstName ?? "";
    public string SelectedLastName => FoundCustomer?.LastName ?? FoundBuyer?.LastName ?? "";
    public string SelectedCompanyName => FoundCustomer?.CompanyName ?? FoundBuyer?.CompanyName ?? "";
    public string SelectedSiteName => FoundCustomer?.SiteName ?? FoundBuyer?.SiteName ?? "";
    public string SelectedAccountNumberFormatted => FoundCustomer?.AccountNumberFormatted ?? FoundBuyer?.AccountNumberDisplay ?? "";
    public string SelectedIdNumber => FoundCustomer?.IdNumber ?? FoundBuyer?.IdNumber ?? "";
    public long? SelectedAccountNumber => FoundCustomer?.AccountNumber ?? FoundBuyer?.AccountNumber;
    public string SelectedPhoneNumber => FoundCustomer?.PhoneNumber ?? FoundBuyer?.PhoneNumber ?? "";
    public string SelectedMobileNumber => FoundCustomer?.MobileNumber ?? FoundBuyer?.MobileNumber ?? "";
    public string SelectedEmail => FoundCustomer?.Email ?? FoundBuyer?.Email ?? "";
    public bool SelectedTaxable => FoundCustomer?.Taxable ?? FoundBuyer?.Taxable ?? false;

    // --- Validation Logic ---
    public bool IsNewCustomerFullNameInvalid => string.IsNullOrWhiteSpace(NewFirstName) && string.IsNullOrWhiteSpace(NewLastName) && string.IsNullOrWhiteSpace(NewCompanyName);
    public bool IsNewBuyerFullNameInvalid => string.IsNullOrWhiteSpace(NewFirstName) && string.IsNullOrWhiteSpace(NewLastName) && string.IsNullOrWhiteSpace(NewCompanyName);
    public bool HasUnsavedNewCustomer => !string.IsNullOrWhiteSpace(NewFirstName) || !string.IsNullOrWhiteSpace(NewLastName) || !string.IsNullOrWhiteSpace(NewCompanyName);
    public bool HasUnsavedChanges => HasUnsavedNewCustomer;
    public bool CanCreateCustomer => !IsNewCustomerFullNameInvalid;
    public bool CanCreateBuyer => !IsNewBuyerFullNameInvalid;
    public bool CanUpdateCustomer => !IsNewCustomerFullNameInvalid;
    public bool CanUpdateBuyer => !IsNewBuyerFullNameInvalid;
    public bool IsNewBuyerOnlyEnabled => !EnforceBuyerCompany;

    private void NotifySelectedEntityProperties()
    {
        OnPropertyChanged(nameof(SelectedFirstName));
        OnPropertyChanged(nameof(SelectedLastName));
        OnPropertyChanged(nameof(SelectedCompanyName));
        OnPropertyChanged(nameof(SelectedSiteName));
        OnPropertyChanged(nameof(SelectedAccountNumberFormatted));
        OnPropertyChanged(nameof(SelectedIdNumber));
        OnPropertyChanged(nameof(SelectedAccountNumber));
        OnPropertyChanged(nameof(SelectedPhoneNumber));
        OnPropertyChanged(nameof(SelectedMobileNumber));
        OnPropertyChanged(nameof(SelectedEmail));
        OnPropertyChanged(nameof(SelectedTaxable));
        OnPropertyChanged(nameof(SelectedCustomerIdDisplay));
        OnPropertyChanged(nameof(SelectedBuyerIdDisplay));

        // Notify form fields too so Create/Edit updates
        OnPropertyChanged(nameof(NewFirstName));
        OnPropertyChanged(nameof(NewLastName));
        OnPropertyChanged(nameof(NewCompanyName));
        OnPropertyChanged(nameof(NewIdNumber));
        OnPropertyChanged(nameof(NewEmail));
        OnPropertyChanged(nameof(NewPhoneNumber));
        OnPropertyChanged(nameof(NewMobileNumber));
        OnPropertyChanged(nameof(NewTaxable));
        OnPropertyChanged(nameof(NewAccountNumber));
        OnPropertyChanged(nameof(NewAccountNumberDisplay));
        OnPropertyChanged(nameof(NewIsCompany));
        OnPropertyChanged(nameof(SelectedNewProductPriceList));
        OnPropertyChanged(nameof(SelectedNewCompany));
        OnPropertyChanged(nameof(SelectedNewSite));
        
        OnPropertyChanged(nameof(IdCardImage));
        OnPropertyChanged(nameof(DriverLicenseImage));
        OnPropertyChanged(nameof(PhotoImage));
        OnPropertyChanged(nameof(SignatureImage));
        OnPropertyChanged(nameof(FingerprintImage));
    }

    // --- Image Preview Properties ---
    private Bitmap? _idCardImage;
    public Bitmap? IdCardImage { get => _idCardImage; set { _idCardImage = value; OnPropertyChanged(); } }
    private Bitmap? _driverLicenseImage;
    public Bitmap? DriverLicenseImage { get => _driverLicenseImage; set { _driverLicenseImage = value; OnPropertyChanged(); } }
    private Bitmap? _photoImage;
    public Bitmap? PhotoImage { get => _photoImage; set { _photoImage = value; OnPropertyChanged(); } }
    private Bitmap? _signatureImage;
    public Bitmap? SignatureImage { get => _signatureImage; set { _signatureImage = value; OnPropertyChanged(); } }
    private Bitmap? _fingerprintImage;
    public Bitmap? FingerprintImage { get => _fingerprintImage; set { _fingerprintImage = value; OnPropertyChanged(); } }

    private Bitmap? _selectedIdCardImage;
    public Bitmap? SelectedIdCardImage { get => _selectedIdCardImage; set { _selectedIdCardImage = value; OnPropertyChanged(); } }
    private Bitmap? _selectedDriverLicenseImage;
    public Bitmap? SelectedDriverLicenseImage { get => _selectedDriverLicenseImage; set { _selectedDriverLicenseImage = value; OnPropertyChanged(); } }
    private Bitmap? _selectedPhotoImage;
    public Bitmap? SelectedPhotoImage { get => _selectedPhotoImage; set { _selectedPhotoImage = value; OnPropertyChanged(); } }
    private Bitmap? _selectedSignatureImage;
    public Bitmap? SelectedSignatureImage { get => _selectedSignatureImage; set { _selectedSignatureImage = value; OnPropertyChanged(); } }
    private Bitmap? _selectedFingerprintImage;
    public Bitmap? SelectedFingerprintImage { get => _selectedFingerprintImage; set { _selectedFingerprintImage = value; OnPropertyChanged(); } }
}
