using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Buyers;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    public bool IsNewBuyerFullNameInvalid =>
        string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewLastName);

    // --- Buyer details bindings used in BuyersView.axaml ---

    public BuyerDto? SelectedBuyer => FoundBuyer;

    public string SelectedBuyerIdDisplay => FoundBuyer?.BuyerId.ToString() ?? "";
    public string SelectedBuyerFirstName => FoundBuyer?.FirstName ?? "";
    public string SelectedBuyerLastName => FoundBuyer?.LastName ?? "";
    public string SelectedBuyerCompanyName => FoundBuyer?.CompanyName ?? "";
    public string SelectedBuyerSiteName => FoundBuyer?.SiteName ?? "";
    public string SelectedBuyerTaxable => (FoundBuyer?.IsTaxable ?? false) ? "Yes" : "No";
    public string SelectedBuyerIdNumber => FoundBuyer?.IdNumber ?? "";
    public string SelectedBuyerAccountNumberFormatted => FoundBuyer?.AccountNumberDisplay ?? "";
    public string SelectedBuyerPriceCode => FoundBuyer?.PriceCode ?? "";
    public string SelectedBuyerPhoneNumber => FoundBuyer?.PhoneNumber ?? "";
    public string SelectedBuyerMobileNumber => FoundBuyer?.MobileNumber ?? "";
    public string SelectedBuyerEmail => FoundBuyer?.Email ?? "";

    public string BuyerSiteAddressSummary => string.Empty; // TODO: derive from Site lookup if needed

    private ObservableCollection<BuyerDto> _buyerSearchResults = new();
    public ObservableCollection<BuyerDto> BuyerSearchResults
    {
        get => _buyerSearchResults;
        set { _buyerSearchResults = value; OnPropertyChanged(); }
    }

    private BuyerDto? _foundBuyer;
    public BuyerDto? FoundBuyer
    {
        get => _foundBuyer;
        set
        {
            _foundBuyer = value;
            OnPropertyChanged();

            // Populate buyer edit form when selecting from results
            if (_foundBuyer != null)
            {
                // Load buyer images for the details panel
                _ = LoadSelectedBuyerImagesAsync(_foundBuyer);

                IsEditMode = true;
                EditingBuyerId = _foundBuyer.BuyerId;

                NewFirstName = _foundBuyer.FirstName ?? string.Empty;
                NewLastName = _foundBuyer.LastName ?? string.Empty;
                NewIdNumber = _foundBuyer.IdNumber;
                NewPhoneNumber = _foundBuyer.PhoneNumber ?? string.Empty;
                NewMobileNumber = _foundBuyer.MobileNumber ?? string.Empty;
                NewEmail = _foundBuyer.Email ?? string.Empty;
                NewTaxable = _foundBuyer.IsTaxable || _foundBuyer.Taxable;
                NewAccountNumber = _foundBuyer.AccountNumber;

                // Preselect company/site.
                // Important: setting SelectedNewCompany clears sites and loads them async,
                // so we must load sites and then select the site.
                if (_foundBuyer.CompanyId.HasValue)
                {
                    // Preselect company without changing the letter filter.
                    // Changing SelectedNewCompanyLetter can cause the company to fall out of suggestions
                    // and clear selection, which then clears sites.
                    if (NewCompanySuggestions.All(c => c.CompanyId != _foundBuyer.CompanyId.Value))
                    {
                        // Ensure suggestions include all companies temporarily
                        SelectedNewCompanyLetter = "ALL";
                    }

                    // Set pending site selection BEFORE selecting company, because selecting the company
                    // triggers async site loading and clears SelectedNewSite.
                    _pendingSelectSiteId = _foundBuyer.SiteId;

                    SelectedNewCompany = NewCompanySuggestions.FirstOrDefault(c => c.CompanyId == _foundBuyer.CompanyId.Value);
                }
                else
                {
                    SelectedNewCompany = null;
                    SelectedNewSite = null;
                }

                // Preselect price code option
                if (!string.IsNullOrWhiteSpace(_foundBuyer.PriceCode))
                    SelectedPriceCodeChar = PriceCodeOptions.FirstOrDefault(p => p.Code == _foundBuyer.PriceCode);

                OnPropertyChanged(nameof(NewAccountNumberDisplay));
                OnPropertyChanged(nameof(IsNewBuyerFullNameInvalid));
                OnPropertyChanged(nameof(CanCreateBuyer));
                (UpdateBuyerCommand as IAsyncRelayCommand)?.NotifyCanExecuteChanged();
            }

            OnPropertyChanged(nameof(SelectedBuyer));
            OnPropertyChanged(nameof(SelectedBuyerIdDisplay));
            OnPropertyChanged(nameof(SelectedBuyerFirstName));
            OnPropertyChanged(nameof(SelectedBuyerLastName));
            OnPropertyChanged(nameof(SelectedBuyerCompanyName));
            OnPropertyChanged(nameof(SelectedBuyerSiteName));
            OnPropertyChanged(nameof(SelectedBuyerTaxable));
            OnPropertyChanged(nameof(SelectedBuyerIdNumber));
            OnPropertyChanged(nameof(SelectedBuyerAccountNumberFormatted));
            OnPropertyChanged(nameof(SelectedBuyerPriceCode));
            OnPropertyChanged(nameof(SelectedBuyerPhoneNumber));
            OnPropertyChanged(nameof(SelectedBuyerMobileNumber));
            OnPropertyChanged(nameof(SelectedBuyerEmail));
            OnPropertyChanged(nameof(BuyerSiteAddressSummary));
        }
    }
}
