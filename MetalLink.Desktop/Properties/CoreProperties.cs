using System;
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
            if (_currentSection == value) return;

            _previousSection = _currentSection;
            _currentSection = value;

            // if target index < previous index we treat as "back"
            IsSlideFromLeft = (int)_currentSection < (int)_previousSection;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashboardSectionVisible));
            OnPropertyChanged(nameof(IsCustomerSectionVisible));
            OnPropertyChanged(nameof(IsTicketSectionVisible));
            OnPropertyChanged(nameof(IsDocumentSectionVisible));
            OnPropertyChanged(nameof(IsCameraSectionVisible));
        }
    }

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
    public bool IsTicketSectionVisible    => CurrentSection == EnumMainSection.Tickets;
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

    public bool HasUnsavedChanges => HasUnsavedNewCustomer || HasUnsavedTicket;
}
