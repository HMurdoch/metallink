using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Tickets.Sending;
using CommunityToolkit.Mvvm.Input;

namespace MetalLink.Desktop.ViewModels.Sending;

public class SendingLineRow { }

public sealed class TicketsSendingViewModel : ViewModelBase
{
    private readonly TicketSendingService _ticketSendingService;
    private readonly CompanyAndSiteService _companyAndSiteService;
    private readonly ProductsAndPricesService _productsAndPricesService;

    // --- UI Toggles (Isolated) ---
    private bool _sendingIsSearchCriteriaExpanded = true;
    public bool SendingIsSearchCriteriaExpanded { get => _sendingIsSearchCriteriaExpanded; set { _sendingIsSearchCriteriaExpanded = value; OnPropertyChanged(); } }
    
    private bool _sendingIsSearchResultsExpanded;
    public bool SendingIsSearchResultsExpanded { get => _sendingIsSearchResultsExpanded; set { _sendingIsSearchResultsExpanded = value; OnPropertyChanged(); } }
    
    private bool _sendingIsDetailsExpanded;
    public bool SendingIsDetailsExpanded { get => _sendingIsDetailsExpanded; set { _sendingIsDetailsExpanded = value; OnPropertyChanged(); } }
    
    private bool _sendingIsCreateEditExpanded = true;
    public bool SendingIsCreateEditExpanded { get => _sendingIsCreateEditExpanded; set { _sendingIsCreateEditExpanded = value; OnPropertyChanged(); } }
    
    private bool _sendingIsPanelExpanded = true;
    public bool SendingIsPanelExpanded { get => _sendingIsPanelExpanded; set { _sendingIsPanelExpanded = value; OnPropertyChanged(); } }

    public TicketsSendingViewModel(TicketSendingService ticketSendingService, CompanyAndSiteService companyAndSiteService, ProductsAndPricesService productsAndPricesService)
    {
        _ticketSendingService = ticketSendingService;
        _companyAndSiteService = companyAndSiteService;
        _productsAndPricesService = productsAndPricesService;
    }

    public async Task SearchSendingTicketsAsync() { await Task.CompletedTask; }
    public void ClearSendingTicketSearch() { }
    public async Task CreateSendingTicketHeaderAsync() { await Task.CompletedTask; }
    public async Task FinalizeSendingTicketAsync() { await Task.CompletedTask; }
    public async Task AddSendingLineAsync() { await Task.CompletedTask; }
    public async Task ReadWeighbridgeAsync() { await Task.CompletedTask; }
    public async Task ReadWeighbridgeSecondAsync() { await Task.CompletedTask; }
    public async Task ReadPlatformAsync() { await Task.CompletedTask; }
    public void ResetWeighbridgeWeights() { }
    public void ResetPlatformWeight() { }
    public async Task PrintSendingTicketAsync() { await Task.CompletedTask; }
    public ICommand? DeleteSendingTicketCommand => null;
    public async Task SaveTicketAsync() { await Task.CompletedTask; }
    public async Task ClearSendingTicketAsync() { await Task.CompletedTask; }
    public async Task CaptureWeightAsync() { await Task.CompletedTask; }
    public async Task CapturePlatePhotoAsync() { await Task.CompletedTask; }
    public async Task CaptureLoadPhotoAsync() { await Task.CompletedTask; }
    public void ScrollToAddLines() { }
    public async Task InitializeAsync() { await Task.CompletedTask; }
    public async Task OnEnterTicketsSendingAsync() { await Task.CompletedTask; }
}
