using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MetalLink.Shared.Companies;
using MetalLink.Shared.Sites;
using MetalLink.Shared.Customers;
using MetalLink.Desktop.Services;
using System.Collections.Generic;
using System.Threading;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Lazy-created services (we already have _apiClient in the core partial)
    // ----- Customer -----

    private void OnEditCustomer(Shared.Customers.CustomerDto? customer)
    {
        if (customer == null)
            return;

        EditingCustomerId = customer.CustomerId;
        IsEditMode = true;

        // -----------------------
        // Names (already fixed on API, but keep safe)
        // -----------------------
        NewFirstName = customer.FirstName ?? string.Empty;
        NewLastName = customer.LastName ?? string.Empty;

        // -----------------------
        // Basic contact
        // -----------------------
        NewIdNumber = customer.IdNumber ?? string.Empty;
        NewAccountNumber = customer.AccountNumber;
        NewPriceCode = customer.PriceCode ?? string.Empty;
        NewTaxable = customer.Taxable;
        NewPhoneNumber = customer.PhoneNumber ?? string.Empty;
        NewMobileNumber = customer.MobileNumber ?? string.Empty;
        NewEmail = customer.Email ?? string.Empty;

        // -----------------------
        // Company / site mode
        // -----------------------
        NewIsCompany = customer.IsCompany
                       || customer.CompanyId.HasValue
                       || customer.SiteId.HasValue; // <-- use actual flag

        // Try to locate the company in the cached lookup list.
        // First by ID, then (if needed) by name.
        CompanyLookupDto? company = null;

        SyncPriceCodeDropdownFromNewPriceCode();

        if (customer.CompanyId.HasValue)
        {
            company = _allCompanies
                .FirstOrDefault(c => c.CompanyId == customer.CompanyId.Value);
        }

        if (company == null && !string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            company = _allCompanies
                .FirstOrDefault(c =>
                    string.Equals(c.CompanyName,
                        customer.CompanyName,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (company != null)
        {
            var letter = char.ToUpperInvariant(company.CompanyName?.FirstOrDefault() ?? 'A');
            var letterStr = letter.ToString();

            if (!CompanyLetterFilters.Contains(letterStr))
                letterStr = "ALL";

            // This will rebuild NewCompanySuggestions via ApplyNewCompanyLetterFilter
            SelectedCompanyLetter = letterStr;

            // Set the actual selection used by the Create/Edit combobox
            SelectedNewCompany = company;
        }
        else
        {
            SelectedCompanyLetter = "ALL";
            SelectedNewCompany = null;
        }

        // Load sites for the company and select the correct one
        _pendingSelectSiteId = customer.SiteId;
        OnPropertyChanged(nameof(CanCreateCustomer));
        OnPropertyChanged(nameof(CanUpdateCustomer));
        (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        _ = LoadNewSitesAndSelectAsync(customer.SiteId);
    }

    private void ClearCustomerSearch()
    {
        SearchCustomerIdText = string.Empty;
        SearchSiteIdText = string.Empty;
        SearchFirstNameText = string.Empty;
        SearchLastNameText = string.Empty;
        SearchCompanyNameText = string.Empty;
        SearchIdNumberText = string.Empty;
        SearchAddressLine1Text = string.Empty;
        SearchAddressLine2Text = string.Empty;
        SearchSuburbText = string.Empty;
        SearchCityText = string.Empty;
        SearchPostalCodeText = string.Empty;
        SearchPhoneNumberText = string.Empty;
        SearchMobileNumberText = string.Empty;
        SearchEmailText = string.Empty;

        // ✅ IMPORTANT: reset dropdowns
        SearchPriceCode = null;
        SearchTaxable = true;

        // Optional: reset site/company dropdowns if used
        SelectedSearchCompany = null;
        SelectedSearchSite = null;

        // Optional: reload all customers
        //_ = SearchCustomersAsync();
    }

    private async Task OnDeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null)
            return;

        if (IsBusy)
            return;

        var ok = await ConfirmAsync($"Are you sure you want to delete - {customer.FirstName} {customer.LastName} ?");
        if (!ok)
            return;

        IsBusy = true;
        try
        {
            StatusMessage = "[STATUS] Deleting customer...";

            await _customerService.SoftDeleteCustomerAsync(customer.CustomerId);

            CustomerSearchResults.Remove(customer);

            if (FoundCustomer?.CustomerId == customer.CustomerId)
            {
                FoundCustomer = null;
            }

            // If we were editing this customer, reset the form
            if (EditingCustomerId == customer.CustomerId)
            {
                await ClearNewCustomerFormAsync();
            }

            StatusMessage = $"[STATUS] Customer {customer.FirstName} {customer.LastName} deleted (soft).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Delete customer failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearNewCustomerFormAsync()
    {
        EditingCustomerId = null;
        IsEditMode = false;

        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewIdNumber = string.Empty;

        try
        {
            // assign the next available account number
            NewAccountNumber = await _customerService.GetNextAccountNumberAsync();
        }
        catch (Exception ex)
        {
            // Don't crash the app. Log + fall back to null/empty display.
            Console.WriteLine($"GetNextAccountNumberAsync failed: {ex}");
            NewAccountNumber = null;
        }

        SelectedPriceCodeChar = null;
        NewPhoneNumber = string.Empty;
        NewMobileNumber = string.Empty;
        NewEmail = string.Empty;
        NewAddressLine1 = string.Empty;
        NewAddressLine2 = string.Empty;
        NewSuburb = string.Empty;
        NewCity = string.Empty;
        NewPostalCode = string.Empty;

        NewIsCompany = false;
        SelectedCompanyLetter = "ALL";
        SelectedNewCompany = null;
        NewSiteSuggestions.Clear();
        SelectedNewSite = null;
    }

    private async Task LoadNextAccountNumberAsync()
    {
        try
        {
            // You’ll implement this method on your Desktop CustomerService
            var next = await _customerService.GetNextAccountNumberAsync();
            NewAccountNumber = next;
            OnPropertyChanged(nameof(NewAccountNumberDisplay));
            OnPropertyChanged(nameof(CanCreateCustomer));
        }
        catch
        {
            // optional: keep it null or set a safe default
            NewAccountNumber = null;
            OnPropertyChanged(nameof(NewAccountNumberDisplay));
        }
    }

    private string _searchAccountNumberText = string.Empty;

    public string SearchAccountNumberText
    {
        get => _searchAccountNumberText;
        set
        {
            _searchAccountNumberText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private long? ParseAccountNumberOrNull(string text)
    {
        var t = (text ?? "").Trim();

        if (string.IsNullOrEmpty(t))
            return null;

        // treat "0", "00", "0000" etc as "no filter"
        if (t.All(c => c == '0'))
            return null;

        return long.TryParse(t, out var v) ? v : null;
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
            CustomerId = EditingCustomerId.Value,
            FirstName = NewFirstName,
            LastName = NewLastName,
            IdNumber = NewIdNumber,
            AccountNumber = NewAccountNumber,
            PriceCode = SelectedPriceCodeChar?.Code.Trim(),
            PhoneNumber = NewPhoneNumber,
            MobileNumber = NewMobileNumber,
            Email = NewEmail,
            Taxable = NewTaxable,
            IsCompany = NewIsCompany,

            // We KNOW these are non-null if NewIsCompany is true
            // because of the validation above.
            CompanyId = SelectedNewCompany != null
                ? SelectedNewCompany.CompanyId
                : null, // will be null for non-company customers

            SiteId = SelectedNewSite != null
                ? SelectedNewSite.SiteId
                : null
        };

        await _customerService.UpdateCustomerAsync(dto);
        FoundCustomer = await _customerService.GetCustomerByIdAsync(dto.CustomerId);

        // Pull fresh copy from API (includes SiteName + AddressLine2 etc)
        var refreshed = await _customerService.GetCustomerByIdAsync(dto.CustomerId);

        // Fallback if API returns null for any reason
        refreshed ??= dto;

        var existing = CustomerSearchResults.FirstOrDefault(c => c.CustomerId == dto.CustomerId);
        if (existing != null)
        {
            var index = CustomerSearchResults.IndexOf(existing);
            if (index >= 0)
                CustomerSearchResults[index] = refreshed; // replace item (forces UI refresh)
        }
        else
        {
            CustomerSearchResults.Add(refreshed);
        }

        // update details panel immediately
        FoundCustomer = refreshed;


        await ClearNewCustomerFormAsync();
        _newAccountNumber = await _customerService.GetNextAccountNumberAsync();
        OnPropertyChanged(nameof(NewAccountNumber));
        OnPropertyChanged(nameof(CanCreateCustomer));
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

        StatusMessage =
            $"Logging ticket for customer {customer.FirstName} {customer.LastName} - ({customer.CustomerId:D8}).";
    }

    // =====================================================
    // CREATE CUSTOMER – COMPANY + SITE (LETTER FILTER)
    // =====================================================


    /// <summary>
    /// Rebuilds NewCompanySuggestions based on SelectedNewCompanyLetter.
    /// </summary>
    private string? _selectedNewCompanyLetter = "ALL";

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



    private void UpdateNewLocationFromSelectedSite()
    {
        // Nothing selected – nothing to sync
        if (SelectedNewSite == null)
            return;

        // 🔹 Province: match by Id into the Provinces collection
        if (SelectedNewSite.ProvinceId.HasValue && Provinces is { Count: > 0 })
        {
            var province = Provinces.FirstOrDefault(p => p.ProvinceId == SelectedNewSite.ProvinceId.Value);

            if (province != null)
            {
                NewProvince = province;
            }
        }

        // 🔹 Country: match by Id into the Countries collection
        if (SelectedNewSite.CountryId.HasValue && Countries is { Count: > 0 })
        {
            var country = Countries.FirstOrDefault(c => c.CountryId == SelectedNewSite.CountryId.Value);

            if (country != null)
            {
                NewCountry = country;
            }
        }
    }

    // Which customer (if any) are we editing?
    private long? _editingCustomerId;

    public long? EditingCustomerId
    {
        get => _editingCustomerId;
        set
        {
            if (_editingCustomerId == value) return;
            _editingCustomerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdateCustomer));

            // ✅ IMPORTANT: refresh command CanExecute
            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
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
            OnPropertyChanged(nameof(CanCreateCustomer));
            OnPropertyChanged(nameof(CanUpdateCustomer));
            OnPropertyChanged(nameof(IsCreateMode)); // you already expose IsCreateMode

            // ✅ IMPORTANT: refresh command CanExecute
            (UpdateCustomerCommand as CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    // Convenience flag for binding Create button
    public bool IsCreateMode => !IsEditMode;


}
