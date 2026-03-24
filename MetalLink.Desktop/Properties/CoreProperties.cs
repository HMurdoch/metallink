using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Measure;
using MetalLink.Desktop.Auth;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    private string _title = "Metal Link Desktop";
    private string _statusMessage = "Ready.";
    private bool _isBusy;

    // Menu / sections
    private EnumMainSection _currentSection = EnumMainSection.Dashboard;
    private EnumMainSection _previousSection = EnumMainSection.Dashboard;
    private bool _isSlideFromLeft;

    // Dashboard counters
    private int _customersLoadedCount;
    private int _ticketsCreatedCount;

    // Tab index
    private int _selectedTabIndex;

    // Animated dashboard values
    private int _animatedTotalCustomersInDb;
    private int _animatedTotalTicketsInDb;
    private int _animatedTotalCompaniesInDb;
    private int _animatedTotalSitesInDb;
    private int _animatedTotalProductsInDb;

    // Actual DB counts (not animated)
    private int _totalCustomersInDb;
    private int _totalTicketsInDb;

    // Charts
    public ISeries[] TicketsByTypeSeries { get; set; } = System.Array.Empty<ISeries>();
    public ISeries[] TicketsPerDaySeries { get; set; } = System.Array.Empty<ISeries>();
    public LiveChartsCore.SkiaSharpView.Axis[] TicketsPerDayXAxis { get; set; } = System.Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
            set
        {
            _statusMessage = value ?? string.Empty;
            OnPropertyChanged();

            // ALSO log to console so you see it in dotnet run output:
            Console.WriteLine($"{_statusMessage}");
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public EnumMainSection CurrentSection
    {
        get => _currentSection;
        set
        {
            Console.WriteLine($"[DEBUG] CoreProperties: CurrentSection changing from '{_currentSection}' to '{value}'");
            if (_currentSection == value) return;

            _previousSection = _currentSection;
            _currentSection = value;
            // if target index < previous index we treat as "back"
            IsSlideFromLeft = (int)_currentSection < (int)_previousSection;

            // TicketsReceiving/TicketsSending now manage their own state in isolated viewmodels.
            // No shared shell initialization here.

            Console.WriteLine($"[DEBUG] CoreProperties: Raising OnPropertyChanged for CurrentSection (value: {_currentSection})");
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashboardSectionVisible));
            OnPropertyChanged(nameof(IsCustomerSectionVisible));
            OnPropertyChanged(nameof(IsTicketsReceivingSectionVisible));
            OnPropertyChanged(nameof(IsTicketsSendingSectionVisible));
            OnPropertyChanged(nameof(IsBuyersSectionVisible));
            OnPropertyChanged(nameof(IsCompanyAndSitesSectionVisible));
            OnPropertyChanged(nameof(IsProductsAndPricesSectionVisible));
            OnPropertyChanged(nameof(IsStockLevelsSectionVisible));
            OnPropertyChanged(nameof(IsStockMovementSectionVisible));
            OnPropertyChanged(nameof(IsReportsSectionVisible));
            OnPropertyChanged(nameof(IsSettingsSectionVisible));
            OnPropertyChanged(nameof(IsDocumentSectionVisible));
            OnPropertyChanged(nameof(IsCameraSectionVisible));
            OnPropertyChanged(nameof(TicketsPageHeading));
            Console.WriteLine("[DEBUG] CoreProperties: PropertyChanged events fired for CurrentSection.");
        }
    }

    // Computed property for Tickets page heading
    public string TicketsPageHeading =>
        CurrentSection == EnumMainSection.TicketsSending
            ? "Ticket Sending"
            : "Ticket Receiving";

    public bool IsSlideFromLeft
    {
        get => _isSlideFromLeft;
        set
        {
            if (_isSlideFromLeft == value) return;
            _isSlideFromLeft = value;
            OnPropertyChanged();
        }
    }

    // Section visibility
    public bool IsDashboardSectionVisible => CurrentSection == EnumMainSection.Dashboard;
    public bool IsCustomerSectionVisible  => CurrentSection == EnumMainSection.Customers;
    public bool IsTicketsReceivingSectionVisible => CurrentSection == EnumMainSection.TicketsReceiving;
    public bool IsTicketsSendingSectionVisible => CurrentSection == EnumMainSection.TicketsSending;
    public bool IsBuyersSectionVisible => CurrentSection == EnumMainSection.Buyers;
    public bool IsCompanyAndSitesSectionVisible => CurrentSection == EnumMainSection.CompanyAndSites;
    public bool IsProductsAndPricesSectionVisible => CurrentSection == EnumMainSection.ProductsAndPrices;
    public bool IsStockLevelsSectionVisible => CurrentSection == EnumMainSection.StockLevels;
    public bool IsStockMovementSectionVisible => CurrentSection == EnumMainSection.StockMovement;
    public bool IsReportsSectionVisible => CurrentSection == EnumMainSection.Reports;
    public bool IsSettingsSectionVisible => CurrentSection == EnumMainSection.Settings;

    private bool _isNavCollapsed;
    public bool IsNavCollapsed
    {
        get => _isNavCollapsed;
        set
        {
            if (_isNavCollapsed == value) return;
            _isNavCollapsed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NavColumnWidth));
            OnPropertyChanged(nameof(NavLabelOpacity));
        }
    }

    public double NavColumnWidth => IsNavCollapsed ? 64 : 260;
    public double NavLabelOpacity => IsNavCollapsed ? 0 : 1;

    public bool IsDocumentSectionVisible  => CurrentSection == EnumMainSection.Documents;
    public bool IsCameraSectionVisible    => CurrentSection == EnumMainSection.Camera;

    public string LoggedInUser => $"{_authState.DisplayName} ({_authState.Username})";

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public int CustomersLoadedCount
    {
        get => _customersLoadedCount;
        set { _customersLoadedCount = value; OnPropertyChanged(); }
    }

    public int TicketsCreatedCount
    {
        get => _ticketsCreatedCount;
        set { _ticketsCreatedCount = value; OnPropertyChanged(); }
    }

    public int AnimatedTotalCustomersInDb
    {
        get => _animatedTotalCustomersInDb;
        set { _animatedTotalCustomersInDb = value; OnPropertyChanged(); }
    }

    public int AnimatedTotalTicketsInDb
    {
        get => _animatedTotalTicketsInDb;
        set { _animatedTotalTicketsInDb = value; OnPropertyChanged(); }
    }

    public int AnimatedTotalCompaniesInDb
    {
        get => _animatedTotalCompaniesInDb;
        set { _animatedTotalCompaniesInDb = value; OnPropertyChanged(); }
    }

    public int AnimatedTotalSitesInDb
    {
        get => _animatedTotalSitesInDb;
        set { _animatedTotalSitesInDb = value; OnPropertyChanged(); }
    }

    public int AnimatedTotalProductsInDb
    {
        get => _animatedTotalProductsInDb;
        set { _animatedTotalProductsInDb = value; OnPropertyChanged(); }
    }

    public int TotalCustomersInDb
    {
        get => _totalCustomersInDb;
        set { _totalCustomersInDb = value; OnPropertyChanged(); }
    }

    public int TotalTicketsInDb
    {
        get => _totalTicketsInDb;
        set { _totalTicketsInDb = value; OnPropertyChanged(); }
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set { _pageSize = value; OnPropertyChanged(); }
    }

    private bool _enforceBuyerCompany = true;
    public bool EnforceBuyerCompany
    {
        get => _enforceBuyerCompany;
        set 
        { 
            _enforceBuyerCompany = value; 
            OnPropertyChanged(); 
            if (value && CurrentSection == EnumMainSection.Buyers)
            {
                NewIsCompany = true;
            }
        }
    }

    private bool _playIntroVideo = true;
    public bool PlayIntroVideo
    {
        get => _playIntroVideo;
        set
        {
            if (_playIntroVideo == value) return;
            _playIntroVideo = value;
            OnPropertyChanged();
            _ = SetPlayIntroVideoAsync(value);
        }
    }

    public async Task SetPlayIntroVideoAsync(bool enabled)
    {
        try
        {
            // setting_id 3, option 4 = Yes, 5 = No
            int optionId = enabled ? 4 : 5;
            if (_app != null)
            {
                await _app.OperatorSettingsService.UpdateSettingAsync(3, optionId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SetPlayIntroVideoAsync: {ex.Message}");
        }
    }


}
