using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MetalLink.Desktop.Hardware;
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
        SearchAccountNumberText = string.Empty; // ✅ FIXED: Clear account number field
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

        // Optional: reset site/company dropdowns if used
        SelectedSearchCompany = null;
        SelectedSearchSite = null;
        SelectedCompanyLetter = "ALL"; // ✅ FIXED: Reset company letter filter

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

        // Clear captured images
        IdCardImage = null;
        DriverLicenseImage = null;
        PhotoImage = null;
        SignatureImage = null;
        FingerprintImage = null;
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
            CustomerId = (int)EditingCustomerId.Value,
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
                ? (int?)SelectedNewCompany.CompanyId
                : null, // will be null for non-company customers

            SiteId = SelectedNewSite != null
                ? (int?)SelectedNewSite.SiteId
                : null
        };

        await _customerService.UpdateCustomerAsync(dto);
        
        // Upload images if captured
        await UploadCustomerImagesAsync(dto.CustomerId);
        
        // Pull fresh copy from API AFTER images are uploaded (includes SiteName + image paths etc)
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

        // Update details panel immediately - this will trigger LoadSelectedCustomerImagesAsync
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

        // Generate a fresh ticket number for this new ticket if none is set
        if (string.IsNullOrWhiteSpace(TicketNumber))
        {
            TicketNumber = GenerateNextTicketNumber();
        }

        // Switch to the Tickets section – this uses the same enum
        // you already use in ShowTicketsCommand.
        CurrentSection = EnumMainSection.TicketsReceiving;

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
        if (SelectedNewSite.ProvinceId > 0 && Provinces is { Count: > 0 })
        {
            var province = Provinces.FirstOrDefault(p => p.ProvinceId == SelectedNewSite.ProvinceId);

            if (province != null)
            {
                NewProvince = province;
            }
        }

        // 🔹 Country: match by Id into the Countries collection
        if (SelectedNewSite.CountryId > 0 && Countries is { Count: > 0 })
        {
            var country = Countries.FirstOrDefault(c => c.CountryId == SelectedNewSite.CountryId);

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

    // --- Customer Image Capture Methods ---

    private async Task CaptureIdCardAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Scanning ID card...";

        try
        {
            var result = await _app.DocumentScanner.ScanDocumentAsync(DocumentType.IdCard);
            
            if (result.IsSuccess && result.ImageData != null)
            {
                IdCardImage = LoadBitmapFromBytes(result.ImageData);
                StatusMessage = "✓ ID card scanned successfully";
            }
            else
            {
                StatusMessage = $"Failed to scan ID card: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning ID card: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CaptureDriverLicenseAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Scanning driver license...";

        try
        {
            var result = await _app.DocumentScanner.ScanDocumentAsync(DocumentType.DriverLicense);
            
            if (result.IsSuccess && result.ImageData != null)
            {
                DriverLicenseImage = LoadBitmapFromBytes(result.ImageData);
                StatusMessage = "✓ Driver license scanned successfully";
            }
            else
            {
                StatusMessage = $"Failed to scan driver license: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning driver license: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CapturePhotoAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Capturing photo...";

        try
        {
            var result = await _app.CameraService.CaptureAsync(CameraDeviceType.PlatformFront, "CustomerPhoto");
            
            if (result != null && result.ImageData != null)
            {
                PhotoImage = LoadBitmapFromBytes(result.ImageData);
                StatusMessage = "✓ Photo captured successfully";
            }
            else
            {
                StatusMessage = "Failed to capture photo";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error capturing photo: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CaptureFingerprintAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Scanning fingerprint...";

        try
        {
            var result = await _app.FingerprintScanner.CaptureAsync();
            
            if (result.IsSuccess && result.ImageData != null)
            {
                FingerprintImage = LoadBitmapFromBytes(result.ImageData);
                StatusMessage = $"✓ Fingerprint scanned successfully (Quality: {result.Quality}%)";
            }
            else
            {
                StatusMessage = $"Failed to scan fingerprint: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning fingerprint: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Bitmap? LoadBitmapFromBytes(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private async Task LoadSelectedCustomerImagesAsync(CustomerDto? customer)
    {
        Console.WriteLine($"[DEBUG] LoadSelectedCustomerImagesAsync called for customer {customer?.CustomerId}");
        
        // Clear existing images
        SelectedIdCardImage = null;
        SelectedDriverLicenseImage = null;
        SelectedPhotoImage = null;
        SelectedSignatureImage = null;
        SelectedFingerprintImage = null;

        if (customer == null)
        {
            Console.WriteLine($"[DEBUG] Customer is null, returning");
            return;
        }

        Console.WriteLine($"[DEBUG] Image paths - IdCard: {customer.IdCardImagePath}, DriverLicense: {customer.DriverLicenseImagePath}, Photo: {customer.PhotoImagePath}, Signature: {customer.SignatureImagePath}, Fingerprint: {customer.FingerprintImagePath}");

        try
        {
            // Download and display ID card image
            if (!string.IsNullOrWhiteSpace(customer.IdCardImagePath))
            {
                Console.WriteLine($"[DEBUG] Downloading ID card image...");
                var imageData = await _customerService.DownloadCustomerImageAsync(customer.CustomerId, "idcard");
                Console.WriteLine($"[DEBUG] ID card image data: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    SelectedIdCardImage = LoadBitmapFromBytes(imageData);
                    Console.WriteLine($"[DEBUG] ID card bitmap loaded: {SelectedIdCardImage != null}");
                }
            }

            // Download and display driver license image
            if (!string.IsNullOrWhiteSpace(customer.DriverLicenseImagePath))
            {
                Console.WriteLine($"[DEBUG] Downloading driver license image...");
                var imageData = await _customerService.DownloadCustomerImageAsync(customer.CustomerId, "driverlicense");
                Console.WriteLine($"[DEBUG] Driver license image data: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    SelectedDriverLicenseImage = LoadBitmapFromBytes(imageData);
                    Console.WriteLine($"[DEBUG] Driver license bitmap loaded: {SelectedDriverLicenseImage != null}");
                }
            }

            // Download and display photo
            if (!string.IsNullOrWhiteSpace(customer.PhotoImagePath))
            {
                Console.WriteLine($"[DEBUG] Downloading photo image...");
                var imageData = await _customerService.DownloadCustomerImageAsync(customer.CustomerId, "photo");
                Console.WriteLine($"[DEBUG] Photo image data: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    SelectedPhotoImage = LoadBitmapFromBytes(imageData);
                    Console.WriteLine($"[DEBUG] Photo bitmap loaded: {SelectedPhotoImage != null}");
                }
            }

            // Download and display signature
            if (!string.IsNullOrWhiteSpace(customer.SignatureImagePath))
            {
                Console.WriteLine($"[DEBUG] Downloading signature image...");
                var imageData = await _customerService.DownloadCustomerImageAsync(customer.CustomerId, "signature");
                Console.WriteLine($"[DEBUG] Signature image data: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    SelectedSignatureImage = LoadBitmapFromBytes(imageData);
                    Console.WriteLine($"[DEBUG] Signature bitmap loaded: {SelectedSignatureImage != null}");
                }
            }

            // Download and display fingerprint
            if (!string.IsNullOrWhiteSpace(customer.FingerprintImagePath))
            {
                Console.WriteLine($"[DEBUG] Downloading fingerprint image...");
                var imageData = await _customerService.DownloadCustomerImageAsync(customer.CustomerId, "fingerprint");
                Console.WriteLine($"[DEBUG] Fingerprint image data: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    SelectedFingerprintImage = LoadBitmapFromBytes(imageData);
                    Console.WriteLine($"[DEBUG] Fingerprint bitmap loaded: {SelectedFingerprintImage != null}");
                }
            }
            
            Console.WriteLine($"[DEBUG] LoadSelectedCustomerImagesAsync completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error loading customer images: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        }
    }
}
