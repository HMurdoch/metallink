using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MetalLink.Desktop.Services;
using MetalLink.Desktop.Hardware;
using MetalLink.Shared.Tickets.Receiving;
using CommunityToolkit.Mvvm.Input;

namespace MetalLink.Desktop.ViewModels.Receiving;

public class ReceivingLineRow { }

public sealed class TicketsReceivingViewModel : ViewModelBase
{
    private readonly TicketReceivingService _ticketReceivingService;
    private readonly CompanyAndSiteService _companyAndSiteService;
    private readonly IScaleService _scaleService;
    private readonly ProductsAndPricesService _productsAndPricesService;

    // --- UI Toggles (Isolated) ---
    private bool _receivingIsSearchCriteriaExpanded = true;
    public bool ReceivingIsSearchCriteriaExpanded { get => _receivingIsSearchCriteriaExpanded; set { _receivingIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _receivingIsSearchResultsExpanded;
    public bool ReceivingIsSearchResultsExpanded { get => _receivingIsSearchResultsExpanded; set { _receivingIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    
    private bool _receivingIsDetailsExpanded;
    public bool ReceivingIsDetailsExpanded { get => _receivingIsDetailsExpanded; set { _receivingIsDetailsExpanded = value; OnPropertyChanged(); } }
    
    private bool _receivingIsCreateEditExpanded = true;
    public bool ReceivingIsCreateEditExpanded { get => _receivingIsCreateEditExpanded; set { _receivingIsCreateEditExpanded = value; OnPropertyChanged(); } }
    
    private bool _receivingIsPanelExpanded = true;
    public bool ReceivingIsPanelExpanded { get => _receivingIsPanelExpanded; set { _receivingIsPanelExpanded = value; OnPropertyChanged(); } }

    public TicketsReceivingViewModel(TicketReceivingService ticketReceivingService, CompanyAndSiteService companyAndSiteService, IScaleService scaleService, ProductsAndPricesService productsAndPricesService)
    {
        _ticketReceivingService = ticketReceivingService;
        _companyAndSiteService = companyAndSiteService;
        _scaleService = scaleService;
        _productsAndPricesService = productsAndPricesService;
    }

    public async Task SearchReceivingTicketsAsync() { await Task.CompletedTask; }
    public void ClearReceivingTicketSearch() { }
    public async Task CreateTicketHeaderAsync() { await Task.CompletedTask; }
    public async Task SaveAndResetReceivingTicketAsync() { await Task.CompletedTask; }
    public async Task FinalizeReceivingTicketAsync() { await Task.CompletedTask; }
    public async Task AddReceivingLineAsync() { await Task.CompletedTask; }
    public async Task ReadWeighbridgeAsync() { await Task.CompletedTask; }
    public async Task ReadWeighbridgeSecondAsync() { await Task.CompletedTask; }
    public async Task ReadPlatformAsync() { await Task.CompletedTask; }
    public void ResetWeighbridgeWeights() { }
    public void ResetPlatformWeight() { }
    public void ShowLineNotes(string notes) { }
    public void CloseLineNotes() { }
    public ICommand? DeleteReceivingTicketCommand => null;
    public async Task SaveTicketAsync() { await Task.CompletedTask; }
    public async Task ClearTicketAsync() { await Task.CompletedTask; }
    public async Task CaptureWeightAsync() { await Task.CompletedTask; }
    public async Task CapturePlatePhotoAsync() { await Task.CompletedTask; }
    public async Task CaptureLoadPhotoAsync() { await Task.CompletedTask; }
    public void ScrollToAddLines() { }
    public async Task InitializeAsync() { await Task.CompletedTask; }
    public async Task PrintReceivingTicketAsync() { await Task.CompletedTask; }
}
