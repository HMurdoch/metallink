using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.Measure;
using MetalLink.Desktop;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Customers;
using MetalLink.Shared.Tickets;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    // --- Services / dependencies ---

    private readonly App _app;
    private readonly AuthState _authState;
    private readonly ApiClient _apiClient;
    private readonly CustomerService _customerService;
    private readonly TicketService _ticketService;
    private readonly ProvinceService _provinceService;
    private readonly IScaleService _scaleService;
    private readonly DocumentService _documentService;
    private readonly ICameraService _cameraService;
    private readonly TicketReportService _ticketReportService;
    private readonly ISignaturePadService _signaturePadService;
    public new event PropertyChangedEventHandler? PropertyChanged;

    // Commands
    public ICommand CheckDbCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SearchCustomerCommand { get; }
    public ICommand CreateCustomerCommand { get; }
    public ICommand CreateTicketCommand { get; }
    public ICommand AddReceivingLineCommand { get; }
    public ICommand RemoveReceivingLineCommand { get; }
    public ICommand ReadWeighbridgeCommand { get; }
    public ICommand ReadPlatformCommand { get; }
    public ICommand LoadCustomerDocumentsCommand { get; }
    public ICommand UploadCustomerDocumentCommand { get; }

    // Section navigation
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowCustomersCommand { get; }
    public ICommand ShowCompanyAndSitesCommand { get; }
    public ICommand ShowProductsAndPricesCommand { get; }
    public ICommand ShowTicketsCommand { get; }
    public ICommand ShowTicketsSendingCommand { get; }
    public ICommand ShowDocumentsCommand { get; }
    public ICommand ShowCameraCommand { get; }
    public ICommand ShowReportsCommand { get; }   // ✅ ADDED
    public ICommand ShowSettingsCommand { get; }  // ✅ ADDED

    // Camera commands
    public ICommand CaptureWbFrontBeforeCommand { get; }
    public ICommand CaptureWbTopBeforeCommand { get; }
    public ICommand CaptureWbFrontAfterCommand { get; }
    public ICommand CaptureWbTopAfterCommand { get; }
    public ICommand CapturePfFrontBeforeCommand { get; }
    public ICommand CapturePfTopBeforeCommand { get; }
    public ICommand CapturePfFrontAfterCommand { get; }
    public ICommand CapturePfTopAfterCommand { get; }

    // Ticket Report commands
    public ICommand DownloadTicketReportCommand { get; }

    // Ticket search commands
    public ICommand SearchTicketsCommand { get; }
    public ICommand ClearTicketSearchCommand { get; }
    public ICommand DeleteTicketCommand { get; }
    public ICommand EditTicketCommand { get; }
    public ICommand CancelEditTicketCommand { get; }
    
    // Ticket line commands
    public ICommand EditTicketLineCommand { get; }
    public ICommand DeleteTicketLineCommand { get; }

    // Optional tab navigation commands
    public ICommand GoDashboardCommand { get; }
    public ICommand GoCustomerCommand { get; }
    public ICommand GoTicketsCommand { get; }
    public ICommand GoDocumentsCommand { get; }
    public ICommand GoCameraCommand { get; }

    // Signature command
    public ICommand CaptureSignatureCommand { get; }

    // Ticket receiving commands
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveTicketCommand { get; }
    public ICommand ClearTicketCommand { get; }
    public ICommand CaptureWeightCommand { get; }
    public ICommand CapturePlatePhotoCommand { get; }
    public ICommand CaptureLoadPhotoCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand PrintTicketCommand { get; }
    public ICommand ScrollToAddLinesCommand { get; }
    public ICommand SaveEditedTicketLineCommand { get; }
    public ICommand CancelEditTicketLineCommand { get; }

    // Customer image capture commands
    public ICommand CaptureIdCardCommand { get; }
    public ICommand CaptureDriverLicenseCommand { get; }
    public ICommand CapturePhotoCommand { get; }
    public ICommand CaptureFingerprintCommand { get; }

    public ICommand EditCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand LogTicketCommand { get; }
    public ICommand ClearNewCustomerCommand { get; }
    public ICommand ClearCustomerSearchCommand { get; }
    public ICommand UpdateCustomerCommand { get; }
    public ICommand SearchCustomersCommand { get; }

    public MainWindowViewModel(App app)
    {
        _app = app;
        _authState = app.AuthState;
        _apiClient = app.ApiClient;
        _customerService = app.CustomerService;
        _ticketService = app.TicketService;
        _provinceService = app.ProvinceService;
        _scaleService = app.ScaleService;
        _documentService = app.DocumentService;
        _cameraService = app.CameraService;
        _ticketReportService = app.TicketReportService;
        _signaturePadService = app.SignaturePadService;

        _ = LoadDashboardStatsAsync();

        // Demo – you can later wire these to API stats
        TicketsByTypeSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { 60 }, Name = "Weighbridge" },
            new PieSeries<int> { Values = new[] { 40 }, Name = "Platform" }
        };

        TicketsPerDaySeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Name = "Tickets per day",
                Values = new[] { 4, 7, 3, 9, 5, 2, 8 }
            }
        };

        TicketsPerDayXAxis = new[]
        {
            new Axis
            {
                Labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }
            }
        };

        SelectedTabIndex = 0;
        IsSearchSiteEnabled = false;

        // Core commands
        CheckDbCommand = new AsyncCommand(CheckDbAsync);
        LogoutCommand = new AsyncCommand(LogoutAsync);
        SearchCustomerCommand = new AsyncCommand(SearchCustomerAsync);
        CreateCustomerCommand = new AsyncCommand(CreateCustomerAsync);
        CreateTicketCommand = new AsyncCommand(CreateTicketAsync);
        AddReceivingLineCommand = new AsyncCommand(AddReceivingLineAsync);
        RemoveReceivingLineCommand = new AsyncRelayCommand<ReceivingLineItem?>(RemoveReceivingLineAsync);
        ReadWeighbridgeCommand = new AsyncCommand(ReadWeighbridgeAsync);
        ReadPlatformCommand = new AsyncCommand(ReadPlatformAsync);
        LoadCustomerDocumentsCommand = new AsyncCommand(LoadCustomerDocumentsAsync);
        UploadCustomerDocumentCommand = new AsyncCommand(UploadCustomerDocumentAsync);

        ShowCompanyAndSitesCommand = ReactiveUI.ReactiveCommand.Create(() =>
        {
            CurrentSection = EnumMainSection.CompanyAndSites;
            
            // Trigger company data loading
            _ = CompanyLetterFilters; // Lazy load trigger
        });
        ShowProductsAndPricesCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.ProductsAndPrices);
        // Section navigation (used by menu)
        ShowDashboardCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Dashboard);
        ShowCustomersCommand = ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
        {
            CurrentSection = EnumMainSection.Customers;
            
            // Trigger company data loading for dropdowns
            _ = CompanyLetterFilters; // Lazy load trigger
            
            await ClearNewCustomerFormAsync(); // this fetches NewAccountNumber
        });
        ShowTicketsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Tickets);
        ShowTicketsSendingCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.TicketsSending);
        ShowDocumentsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Documents);
        ShowCameraCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Camera);

        // ✅ ADDED: Reports + Settings behave like other nav items
        ShowReportsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Reports);
        ShowSettingsCommand = ReactiveUI.ReactiveCommand.Create(() => CurrentSection = EnumMainSection.Settings);

        EditCustomerCommand = new RelayCommand<CustomerDto>(OnEditCustomer);
        DeleteCustomerCommand = new AsyncRelayCommand<CustomerDto>(execute: OnDeleteCustomerAsync);
        LogTicketCommand = new RelayCommand<CustomerDto>(OnLogTicket);
        ClearNewCustomerCommand = new AsyncRelayCommand(ClearNewCustomerFormAsync);
        ClearCustomerSearchCommand = new RelayCommand(ClearCustomerSearch);

        Console.WriteLine($"Next account number = {NewAccountNumber}");
        OnPropertyChanged(nameof(NewAccountNumberDisplay));

        UpdateCustomerCommand = new AsyncRelayCommand(OnUpdateCustomerAsync, () => CanUpdateCustomer);
        SearchCustomersCommand = new AsyncRelayCommand(SearchCustomerAsync);

        // Camera commands
        CaptureWbFrontBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeFront, "wb_front_before"));
        CaptureWbTopBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeTop, "wb_top_before"));
        CaptureWbFrontAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeFront, "wb_front_after"));
        CaptureWbTopAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.WeighbridgeTop, "wb_top_after"));

        CapturePfFrontBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformFront, "pf_front_before"));
        CapturePfTopBeforeCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformTop, "pf_top_before"));
        CapturePfFrontAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformFront, "pf_front_after"));
        CapturePfTopAfterCommand = new AsyncCommand(() =>
            CaptureAndUploadAsync(CameraDeviceType.PlatformTop, "pf_top_after"));

        // Ticket Report Command
        DownloadTicketReportCommand = new AsyncCommand(DownloadTicketReportAsync);

        // Ticket search commands
        SearchTicketsCommand = new AsyncCommand(SearchTicketsAsync);
        ClearTicketSearchCommand = new RelayCommand(ClearTicketSearch);
        DeleteTicketCommand = new AsyncRelayCommand<TicketSearchResultDto?>(DeleteTicketAsync);
        EditTicketCommand = new RelayCommand<TicketSearchResultDto>(OnEditTicket);
        CancelEditTicketCommand = new RelayCommand(OnCancelEditTicket);
        
        // Ticket line commands
        EditTicketLineCommand = new RelayCommand<TicketLineDto>(OnEditTicketLine);
        DeleteTicketLineCommand = new AsyncRelayCommand<TicketLineDto?>(DeleteTicketLineAsync);

        // Signature
        CaptureSignatureCommand = new AsyncCommand(CaptureSignatureAsync);

        // Ticket receiving commands
        AddLineCommand = new AsyncCommand(AddReceivingLineAsync);
        RemoveLineCommand = new AsyncRelayCommand<ReceivingLineItem?>(RemoveReceivingLineAsync);
        SaveTicketCommand = new AsyncCommand(SaveTicketAsync);
        ClearTicketCommand = new RelayCommand(ClearTicket);
        CaptureWeightCommand = new AsyncCommand(CaptureWeightAsync);
        CapturePlatePhotoCommand = new AsyncCommand(CapturePlatePhotoAsync);
        CaptureLoadPhotoCommand = new AsyncCommand(CaptureLoadPhotoAsync);
        
        // Ticket search commands (additional)
        ClearSearchCommand = new RelayCommand(ClearTicketSearch);
        PrintTicketCommand = new AsyncCommand(PrintTicketAsync);
        ScrollToAddLinesCommand = new RelayCommand(ScrollToAddLines);
        SaveEditedTicketLineCommand = new AsyncCommand(SaveEditedTicketLineAsync);
        CancelEditTicketLineCommand = new RelayCommand(CancelEditTicketLine);

        // Customer image capture commands
        CaptureIdCardCommand = new AsyncCommand(CaptureIdCardAsync);
        CaptureDriverLicenseCommand = new AsyncCommand(CaptureDriverLicenseAsync);
        CapturePhotoCommand = new AsyncCommand(CapturePhotoAsync);
        CaptureFingerprintCommand = new AsyncCommand(CaptureFingerprintAsync);

        // Optional tab navigation (unused in current XAML but kept for later)
        GoDashboardCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 0;
            return Task.CompletedTask;
        });

        GoCustomerCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 1;
            return Task.CompletedTask;
        });

        GoTicketsCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 2;
            return Task.CompletedTask;
        });

        GoDocumentsCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 3;
            return Task.CompletedTask;
        });

        GoCameraCommand = new AsyncCommand(() =>
        {
            SelectedTabIndex = 4;
            return Task.CompletedTask;
        });

        InitializeCountries();
        InitializeCompanyAndSiteCommands();
        InitializeProductsAndPricesCommands();
        _ = LoadProvincesAsync();
    }

    // --- Core helpers / section switching ---

    public async Task InitializeLookupsAsync()
    {
        InitializeCountries();
        await LoadProvincesAsync();
        await ClearNewCustomerFormAsync();
    }

    private Task SwitchSectionAsync(EnumMainSection section)
    {
        CurrentSection = section;
        StatusMessage = $"Section switched to: {section}.";
        return Task.CompletedTask;
    }

    private async Task CreateTicketAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating ticket...";

        try
        {
            // --- Basic validation ---
            if (!long.TryParse(TicketCustomerIdText, out var customerId) || customerId <= 0)
            {
                StatusMessage = "Customer ID must be a valid positive number.";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketNumber))
            {
                StatusMessage = "Ticket Number is required.";
                return;
            }

            if (!decimal.TryParse(NormalizeDecimalText(TicketUnitPriceText),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var unitPrice) || unitPrice < 0)
            {
                StatusMessage = "Unit price must be a valid non-negative number.";
                return;
            }

            decimal? ParseWeight(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                if (decimal.TryParse(NormalizeDecimalText(text),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var value))
                {
                    return value;
                }

                throw new FormatException($"Invalid weight value: '{text}'.");
            }

            var firstWeight = ParseWeight(TicketFirstWeightText);
            var secondWeight = ParseWeight(TicketSecondWeightText);

            // --- Call API ---
            var dto = await _ticketService.CreateTicketAsync(
                customerId: customerId,
                ticketType: string.IsNullOrWhiteSpace(TicketType) ? "weighbridge" : TicketType.Trim(),
                ticketNumber: TicketNumber.Trim(),
                firstWeightKg: firstWeight,
                secondWeightKg: secondWeight,
                unitPricePerKg: unitPrice,
                currencyCode: string.IsNullOrWhiteSpace(TicketCurrencyCode) ? "ZAR" : TicketCurrencyCode.Trim(),
                productDescription: string.IsNullOrWhiteSpace(TicketProductDescription) ? null : TicketProductDescription.Trim(),
                notes: string.IsNullOrWhiteSpace(TicketNotes) ? null : TicketNotes.Trim(),
                vehicleRegistration: string.IsNullOrWhiteSpace(TicketVehicleRegistration) ? null : TicketVehicleRegistration.Trim(),
                ofmWeighbridgeTicket: string.IsNullOrWhiteSpace(TicketOfmWeighbridgeTicket) ? null : TicketOfmWeighbridgeTicket.Trim(),
                foreignTicket: string.IsNullOrWhiteSpace(TicketForeignTicket) ? null : TicketForeignTicket.Trim(),
                ckNumber: string.IsNullOrWhiteSpace(TicketCkNumber) ? null : TicketCkNumber.Trim()
            );

            if (dto == null)
            {
                StatusMessage = "Ticket create failed - API returned no result.";
                return;
            }

            LastCreatedTicket = dto;
            StatusMessage =
                $"Ticket {dto.TicketNumber} created. Net {dto.NetWeightKg} kg, Total {dto.TotalAmount:0.00} {dto.CurrencyCode}.";

            // Prepare for next ticket
            TicketNumber = GenerateNextTicketNumber();
            TicketFirstWeightText = string.Empty;
            TicketSecondWeightText = string.Empty;
            TicketUnitPriceText = string.Empty;
            TicketProductDescription = string.Empty;
            TicketNotes = string.Empty;
            TicketVehicleRegistration = string.Empty;
            TicketOfmWeighbridgeTicket = string.Empty;
            TicketForeignTicket = string.Empty;
            TicketCkNumber = string.Empty;
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (FormatException ex)
        {
            StatusMessage = ex.Message;
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error creating ticket: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string NormalizeDecimalText(string text)
    {
        // Accept both comma and dot as decimal separators and normalize to invariant culture
        return text.Replace(',', '.').Trim();
    }

    private string GenerateNextTicketNumber()
    {
        // Simple client-side ticket number pattern:
        // WB-<SiteId>-YYYYMMDD-HHMMSS
        var siteId = _authState.SiteId > 0 ? _authState.SiteId : 1;
        return $"WB-{siteId}-{DateTime.Now:yyyyMMdd-HHmmss}";
    }

    private void ScrollToAddLines()
    {
        // TODO: Implement actual scrolling - requires view interaction
        // For now, just provide user feedback
        StatusMessage = "Please scroll down to the 'Add Product Lines' section to add line items";
    }

    private async Task<bool> ConfirmAsync(string message)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return false;

        var dlg = new MetalLink.Desktop.Views.ConfirmDialog(message);
        return await dlg.ShowDialog<bool>(owner);
    }

    private async Task CheckDbAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Checking database...";

        try
        {
            var result = await _apiClient.GetAsync<HealthResponse>("api/health/db");

            if (result is not null)
            {
                StatusMessage = $"DB OK. Customers count: {result.customersCount}";
            }
            else
            {
                StatusMessage = "DB check returned no data.";
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task LogoutAsync()
    {
        _app.AuthService.Logout();

        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loginWindow = new MetalLink.Desktop.Views.LoginWindow
            {
                DataContext = new LoginViewModel(_app)
            };

            var current = desktop.MainWindow;
            desktop.MainWindow = loginWindow;
            current?.Close();
        }

        return Task.CompletedTask;
    }

    // --- Customer search / create ---

    private async Task SearchCustomerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Searching customers...";

        try
        {
            long? customerId = null;
            if (long.TryParse(SearchCustomerIdText, out var cid))
                customerId = cid;

            long? siteId = null;
            if (long.TryParse(SearchSiteIdText, out var sid))
                siteId = sid;

            // 🔹 Province / Country filters: null if "ALL"
            long? provinceId = null;
            if (SearchProvince != null &&
                !string.Equals(SearchProvince.ProvinceName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                provinceId = SearchProvince.ProvinceId;
            }

            long? countryId = null;
            if (SearchCountry != null &&
                !string.Equals(SearchCountry.CountryName, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                countryId = SearchCountry.CountryId;
            }

            // Build a request object with all filters (null / empty = ignore)
            var request = new CustomerSearchRequestDto
            {
                CustomerId = customerId,
                SiteId = siteId,
                FirstName = string.IsNullOrWhiteSpace(SearchFirstNameText) ? null : SearchFirstNameText.Trim(),
                LastName = string.IsNullOrWhiteSpace(SearchLastNameText) ? null : SearchLastNameText.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(SearchCompanyNameText) ? null : SearchCompanyNameText.Trim(),
                IdNumber = string.IsNullOrWhiteSpace(SearchIdNumberText) ? null : SearchIdNumberText.Trim(),
                AccountNumber = ParseAccountNumberOrNull(SearchAccountNumberText),
                PriceCode = string.IsNullOrEmpty(SearchPriceCode?.Code) ? null : SearchPriceCode.Code.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(SearchPhoneNumberText) ? null : SearchPhoneNumberText,
                MobileNumber = string.IsNullOrWhiteSpace(SearchMobileNumberText) ? null : SearchMobileNumberText,
                Email = string.IsNullOrWhiteSpace(SearchEmailText) ? null : SearchEmailText,

                ProvinceId = provinceId,
                CountryId = countryId,
                Taxable = SearchTaxable
            };

            var results = await _customerService.SearchCustomersAsync(request);

            CustomerSearchResults.Clear();
            if (results != null)
            {
                foreach (var c in results)
                    CustomerSearchResults.Add(c);
            }

            if (CustomerSearchResults.Count == 0)
            {
                StatusMessage = "No customers found.";
                FoundCustomer = null;
            }
            else
            {
                StatusMessage = $"Found {CustomerSearchResults.Count} customer(s).";
                FoundCustomer = CustomerSearchResults[0];
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateCustomerAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Creating customer...";

        try
        {
            var errors = new List<string>();

            // 1) REQUIRED FIELDS
            if (string.IsNullOrWhiteSpace(NewFirstName))
                errors.Add("First Name is required.");

            if (string.IsNullOrWhiteSpace(NewLastName))
                errors.Add("Last Name is required.");

            if (string.IsNullOrWhiteSpace(NewIdNumber))
                errors.Add("ID Number is required.");

            if (NewAccountNumber == null)
                errors.Add("Account Number is required.");

            if (string.IsNullOrWhiteSpace(NewEmail))
                errors.Add("Email is required.");

            if (NewProvince == null)
                errors.Add("Province is required.");

            if (NewCountry == null)
                errors.Add("Country is required.");

            // 2) COMPANY + SITE RULE
            if (NewIsCompany)
            {
                if (SelectedNewCompany == null || SelectedNewSite == null)
                {
                    StatusMessage = "Company and Site are required when Is Company is checked.";
                    return;
                }

                // Guard: stop “Elementech + Orange Farms” mismatch
                if (SelectedNewSite.CompanyId != SelectedNewCompany.CompanyId)
                {
                    StatusMessage = "Selected Site does not belong to the selected Company. Please pick a matching Site.";
                    return;
                }
            }

            if (errors.Count > 0)
            {
                StatusMessage = string.Join(Environment.NewLine, errors);
                return;
            }

            // 3) UNIQUENESS CHECK (ID NUMBER, ACCOUNT NUMBER, EMAIL)
            var uniqueCheckRequest = new CustomerSearchRequestDto
            {
                // only send the fields we care about for uniqueness
                IdNumber = NewIdNumber,
                Email = NewEmail
            };

            var duplicates = await _customerService.SearchCustomersAsync(uniqueCheckRequest);

            if (duplicates != null && duplicates.Any())
            {
                if (!string.IsNullOrWhiteSpace(NewIdNumber) &&
                    duplicates.Any(c => string.Equals(c.IdNumber, NewIdNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add("A customer with this ID Number already exists.");
                }

                if (!string.IsNullOrWhiteSpace(NewEmail) &&
                    duplicates.Any(c => string.Equals(c.Email, NewEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add("A customer with this Email already exists.");
                }

                if (errors.Count > 0)
                {
                    StatusMessage = string.Join(Environment.NewLine, errors);
                    return;
                }
            }

            if (errors.Count > 0)
            {
                StatusMessage = string.Join(" ", errors);
                return;
            }

            // ----- build DTO for the API (no AccountNumber) -----
            var dto = new CustomerDto
            {
                FirstName = NewFirstName!,
                LastName = NewLastName!,
                IdNumber = NewIdNumber!,
                Email = NewEmail!,
                PhoneNumber = string.IsNullOrWhiteSpace(NewPhoneNumber) ? null : NewPhoneNumber,
                MobileNumber = string.IsNullOrWhiteSpace(NewMobileNumber) ? null : NewMobileNumber,
                PriceCode = string.IsNullOrEmpty(SelectedPriceCodeChar?.Code) ? null : SelectedPriceCodeChar.Code.Trim(),
                IsCompany = NewIsCompany,
                Taxable = NewTaxable,

                CompanyId = NewIsCompany && SelectedNewCompany != null
                                    ? SelectedNewCompany.CompanyId
                                    : null,
                SiteId = NewIsCompany && SelectedNewSite != null
                                    ? SelectedNewSite.SiteId
                                    : null
            };

            Console.WriteLine($"CreateCustomer: IsCompany={NewIsCompany}, CompanyId={SelectedNewCompany?.CompanyId}, SiteId={SelectedNewSite?.SiteId}");

            // API will allocate AccountNumber from the DB identity
            var created = await _customerService.CreateCustomerAsync(dto);
            
            if (created == null)
            {
                StatusMessage = "Customer create failed - API returned no result.";
                return;
            }

            // Upload images if captured
            await UploadCustomerImagesAsync(created.CustomerId);

            // store raw number and refresh displayed padded text
            _newAccountNumber = created.AccountNumber;
            OnPropertyChanged(nameof(NewAccountNumber));

            StatusMessage = $"Customer {created.FirstName} {created.LastName} created successfully (Account {NewAccountNumber}).";
    
            var refreshed = await _customerService.GetCustomerByIdAsync(created.CustomerId);
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
            
            // if you want the form cleared except the new account number, you can adjust here;
            // right now you probably still call ClearNewCustomerForm();
            await ClearNewCustomerFormAsync();
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool CanCreateCustomer =>
        !string.IsNullOrWhiteSpace(NewFirstName)
        && !string.IsNullOrWhiteSpace(NewLastName)
        && (!NewIsCompany || (SelectedNewCompany != null && SelectedNewSite != null))
        && NewAccountNumber.HasValue;

    public bool CanUpdateCustomer =>
        IsEditMode
        && EditingCustomerId.HasValue
        && !string.IsNullOrWhiteSpace(NewFirstName)
        && !string.IsNullOrWhiteSpace(NewLastName);

    // --- Scale reading ---

    private async Task ReadWeighbridgeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Reading weighbridge...";

        try
        {
            var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Weighbridge);
            if (reading == null)
            {
                StatusMessage = "No reading from weighbridge.";
                return;
            }

            if (string.IsNullOrWhiteSpace(TicketFirstWeightText))
            {
                TicketFirstWeightText = reading.WeightKg.ToString("0.0");
                StatusMessage = $"Weighbridge first weight: {reading.WeightKg:0.0} kg.";
            }
            else
            {
                TicketSecondWeightText = reading.WeightKg.ToString("0.0");
                StatusMessage = $"Weighbridge second weight: {reading.WeightKg:0.0} kg.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading weighbridge: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReadPlatformAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Reading platform scale...";

        try
        {
            var reading = await _scaleService.ReadOnceAsync(ScaleDeviceType.Platform);
            if (reading == null)
            {
                StatusMessage = "No reading from platform scale.";
                return;
            }

            TicketFirstWeightText = reading.WeightKg.ToString("0.0");
            StatusMessage = $"Platform weight: {reading.WeightKg:0.0} kg.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading platform scale: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Documents ---

    private async Task LoadCustomerDocumentsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Loading customer documents...";

        try
        {
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID for documents.";
                DocumentsSummary = "No documents loaded.";
                return;
            }

            var docs = await _documentService.GetDocumentsAsync(customerId);

            if (docs == null || docs.Count == 0)
            {
                DocumentsSummary = "No documents found for this customer.";
                StatusMessage = "No documents found.";
                return;
            }

            var lines = docs
                .OrderBy(d => d.CreatedTime)
                .Select(d =>
                    $"ID: {d.CustomerDocumentId}, Type: {d.DocumentType}, File: {d.FileName}, Created: {d.CreatedTime:yyyy-MM-dd HH:mm}, Url: {d.Url}");

            DocumentsSummary = string.Join(Environment.NewLine, lines);
            StatusMessage = $"Loaded {docs.Count} document(s).";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
            DocumentsSummary = "Error loading documents.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UploadCustomerDocumentAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Uploading document...";

        try
        {
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID for documents.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewDocumentType))
            {
                StatusMessage = "Document type is required (e.g. id_front, id_back, signature).";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewDocumentFilePath))
            {
                StatusMessage = "File path is required.";
                return;
            }

            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                NewDocumentType,
                NewDocumentFilePath
            );

            if (doc == null)
            {
                StatusMessage = "Document upload failed (no response).";
                return;
            }

            StatusMessage = $"Uploaded document {doc.FileName} as {doc.DocumentType}.";
            await LoadCustomerDocumentsAsync();

            NewDocumentFilePath = string.Empty;
        }
        catch (FileNotFoundException ex)
        {
            StatusMessage = $"File not found: {ex.FileName}";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error uploading document: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Camera / capture ---

    private async Task CaptureAndUploadAsync(CameraDeviceType deviceType, string documentType)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Capturing image and uploading...";

        try
        {
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage = "Please enter a valid numeric Customer ID (in Customer Documents section) before capturing.";
                return;
            }

            var capture = await _cameraService.CaptureAsync(deviceType, documentType);
            LastCameraCaptureSummary = capture.ToString();

            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                capture.DocumentType,
                capture.FilePath
            );

            if (doc == null)
            {
                StatusMessage = "Camera capture upload failed (no response).";
                return;
            }

            StatusMessage = $"Captured and uploaded {capture.DocumentType} from {deviceType}.";
            await LoadCustomerDocumentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during capture/upload: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Ticket report ---

    private async Task DownloadTicketReportAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Downloading ticket PDF...";

        try
        {
            if (!long.TryParse(TicketReportTicketIdText, out var ticketId))
            {
                StatusMessage = "Please enter a valid numeric Ticket ID.";
                return;
            }

            var path = await _ticketReportService.DownloadTicketReportAsync(ticketId);
            LastTicketReportPath = path;
            StatusMessage = $"Ticket PDF saved to: {path}";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Error calling API: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error downloading ticket report: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Signature ---

    private async Task CaptureSignatureAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Check if we're in the Customer creation/edit section
            if (CurrentSection == EnumMainSection.Customers)
            {
                StatusMessage = "Capturing signature...";
                
                var capture = await _signaturePadService.CaptureAsync("CustomerSignature");
                
                if (capture != null && capture.ImageData != null)
                {
                    SignatureImage = LoadBitmapFromBytes(capture.ImageData);
                    StatusMessage = "✓ Signature captured successfully";
                }
                else
                {
                    StatusMessage = "Failed to capture signature";
                }
                return;
            }

            // Otherwise, we're in the Documents section - need a customer ID
            StatusMessage = "Capturing signature and uploading...";

            // Use the same Customer ID as the Customer Documents section
            if (!long.TryParse(DocumentsCustomerIdText, out var customerId))
            {
                StatusMessage =
                    "Please enter a valid numeric Customer ID in the Customer Documents section before capturing signature.";
                return;
            }

            const string documentType = "signature";

            // Simulate pad capture (currently using MockSignaturePadService)
            var capture2 = await _signaturePadService.CaptureAsync(documentType);
            LastSignatureCaptureSummary = capture2.ToString();

            // Upload as a normal customer document
            var doc = await _documentService.UploadDocumentAsync(
                customerId,
                capture2.DocumentType,
                capture2.FilePath);

            if (doc == null)
            {
                StatusMessage = "Signature upload failed (no response).";
                return;
            }

            StatusMessage = "Signature captured and uploaded.";

            // Refresh the documents list so the new signature appears
            await LoadCustomerDocumentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during signature capture/upload: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Dashboard stats / animation ---

    private async Task LoadDashboardStatsAsync()
    {
        var health = await _apiClient.GetAsync<HealthResponse>("api/health/db");

        if (health != null)
        {
            TotalCustomersInDb = health.customersCount;
            TotalTicketsInDb = health.ticketsCount;
            TotalCompaniesInDb = health.companiesCount;
            TotalSitesInDb = health.sitesCount;
            TotalProductsInDb = health.productsCount;

            _ = AnimateCounterAsync(TotalCustomersInDb, v => AnimatedTotalCustomersInDb = v);
            _ = AnimateCounterAsync(TotalTicketsInDb, v => AnimatedTotalTicketsInDb = v);
            _ = AnimateCounterAsync(TotalCompaniesInDb, v => AnimatedTotalCompaniesInDb = v);
            _ = AnimateCounterAsync(TotalSitesInDb, v => AnimatedTotalSitesInDb = v);
            _ = AnimateCounterAsync(TotalProductsInDb, v => AnimatedTotalProductsInDb = v);
        }
    }

    private async Task AnimateCounterAsync(
        int target,
        Action<int> setValue,
        int durationMs = 600)
    {
        if (target < 0) target = 0;

        var frames = Math.Max(1, durationMs / 30); // ~30 fps
        var step = (double)target / frames;

        double current = 0;

        for (int i = 0; i < frames; i++)
        {
            current += step;
            setValue((int)Math.Round(current));
            await Task.Delay(30);
        }

        setValue(target);
    }

    // --- OnPropertyChanged override ---

    protected new void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // --- Image upload helpers ---

    private async Task UploadCustomerImagesAsync(long customerId)
    {
        Console.WriteLine($"[DEBUG] UploadCustomerImagesAsync called for customer {customerId}");
        Console.WriteLine($"[DEBUG] IdCardImage: {(IdCardImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] DriverLicenseImage: {(DriverLicenseImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] PhotoImage: {(PhotoImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] SignatureImage: {(SignatureImage != null ? "present" : "null")}");
        Console.WriteLine($"[DEBUG] FingerprintImage: {(FingerprintImage != null ? "present" : "null")}");
        
        try
        {
            // Upload ID card if captured
            if (IdCardImage != null)
            {
                Console.WriteLine($"[DEBUG] Converting IdCardImage to bytes...");
                var imageData = BitmapToBytes(IdCardImage);
                Console.WriteLine($"[DEBUG] IdCard imageData size: {imageData?.Length ?? 0} bytes");
                if (imageData != null)
                {
                    Console.WriteLine($"[DEBUG] Uploading ID card...");
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "idcard", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] ID card uploaded successfully");
                }
            }

            // Upload driver license if captured
            if (DriverLicenseImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading driver license...");
                var imageData = BitmapToBytes(DriverLicenseImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "driverlicense", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Driver license uploaded successfully");
                }
            }

            // Upload photo if captured
            if (PhotoImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading photo...");
                var imageData = BitmapToBytes(PhotoImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "photo", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Photo uploaded successfully");
                }
            }

            // Upload signature if captured
            if (SignatureImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading signature...");
                var imageData = BitmapToBytes(SignatureImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "signature", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Signature uploaded successfully");
                }
            }

            // Upload fingerprint if captured
            if (FingerprintImage != null)
            {
                Console.WriteLine($"[DEBUG] Uploading fingerprint...");
                var imageData = BitmapToBytes(FingerprintImage);
                if (imageData != null)
                {
                    await _customerService.UploadCustomerImageAsync(
                        customerId, "fingerprint", imageData, "image/png");
                    Console.WriteLine($"[DEBUG] Fingerprint uploaded successfully");
                }
            }
            
            Console.WriteLine($"[DEBUG] UploadCustomerImagesAsync completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error uploading images: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            // Don't throw - images are optional
        }
    }

    private byte[]? BitmapToBytes(Avalonia.Media.Imaging.Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream);
            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }

    // --- Nested helpers ---

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public AsyncCommand(Func<Task> execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public async void Execute(object? parameter) => await _execute();
    }

    private sealed class HealthResponse
    {
        public string status { get; set; } = string.Empty;
        public int customersCount { get; set; }
        public int ticketsCount { get; set; }
        public int companiesCount { get; set; }
        public int sitesCount { get; set; }
        public int productsCount { get; set; }
    }
}
