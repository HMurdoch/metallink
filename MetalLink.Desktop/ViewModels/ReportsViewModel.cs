using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using MetalLink.Desktop.Configuration;
using MetalLink.Desktop.Services;
using MetalLink.Shared.Products;
using MetalLink.Shared.Sites;
using ReactiveUI;

namespace MetalLink.Desktop.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly ApiClient _apiClient;
    private DateTimeOffset? _stockMovementFromDate;
    private DateTimeOffset? _stockMovementToDate;
    private ProductLookupDto? _selectedProduct;
    private SiteLookupDto? _selectedSite;
    private ObservableCollection<ProductLookupDto> _products = new();
    private ObservableCollection<SiteLookupDto> _sites = new();

    public ReportsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        
        // Set default date range (last 30 days)
        _stockMovementToDate = DateTimeOffset.Now;
        _stockMovementFromDate = DateTimeOffset.Now.AddDays(-30);

        // Initialize commands
        ViewStockOnHandCommand = ReactiveCommand.CreateFromTask(ViewStockOnHandAsync);
        ExportStockOnHandPdfCommand = ReactiveCommand.CreateFromTask(ExportStockOnHandPdfAsync);
        ViewStockMovementCommand = ReactiveCommand.CreateFromTask(ViewStockMovementAsync);
        ExportStockMovementPdfCommand = ReactiveCommand.CreateFromTask(ExportStockMovementPdfAsync);
        RecalculateStockCommand = ReactiveCommand.CreateFromTask(RecalculateStockAsync);
        GenerateTicketReportCommand = ReactiveCommand.CreateFromTask(GenerateTicketReportAsync);

        // Load initial data
        _ = LoadProductsAndSitesAsync();
    }

    #region Properties

    public DateTimeOffset? StockMovementFromDate
    {
        get => _stockMovementFromDate;
        set => SetProperty(ref _stockMovementFromDate, value);
    }

    public DateTimeOffset? StockMovementToDate
    {
        get => _stockMovementToDate;
        set => SetProperty(ref _stockMovementToDate, value);
    }

    public ProductLookupDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public SiteLookupDto? SelectedSite
    {
        get => _selectedSite;
        set => SetProperty(ref _selectedSite, value);
    }

    public ObservableCollection<ProductLookupDto> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ObservableCollection<SiteLookupDto> Sites
    {
        get => _sites;
        set => SetProperty(ref _sites, value);
    }

    #endregion

    #region Commands

    public ICommand ViewStockOnHandCommand { get; }
    public ICommand ExportStockOnHandPdfCommand { get; }
    public ICommand ViewStockMovementCommand { get; }
    public ICommand ExportStockMovementPdfCommand { get; }
    public ICommand RecalculateStockCommand { get; }
    public ICommand GenerateTicketReportCommand { get; }

    #endregion

    #region Methods

    private async Task LoadProductsAndSitesAsync()
    {
        try
        {
            // Load products
            var productsResponse = await _apiClient.GetAsync<ProductLookupDto[]>("products");
            if (productsResponse != null)
            {
                Products = new ObservableCollection<ProductLookupDto>(productsResponse);
            }

            // Load sites
            var sitesResponse = await _apiClient.GetAsync<SiteLookupDto[]>("sites");
            if (sitesResponse != null)
            {
                Sites = new ObservableCollection<SiteLookupDto>(sitesResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading products/sites: {ex.Message}");
        }
    }

    private async Task ViewStockOnHandAsync()
    {
        try
        {
            var siteParam = SelectedSite != null ? $"?siteId={SelectedSite.SiteId}" : "";
            var url = $"{ApiConfig.BaseUrl}/stockreports/stock-on-hand{siteParam}";
            
            // Open in browser
            await OpenUrlInBrowserAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error viewing stock on hand: {ex.Message}");
            // TODO: Show error dialog
        }
    }

    private async Task ExportStockOnHandPdfAsync()
    {
        try
        {
            var siteParam = SelectedSite != null ? $"?siteId={SelectedSite.SiteId}" : "";
            var url = $"{ApiConfig.BaseUrl}/stockreports/stock-on-hand/pdf{siteParam}";
            
            // Open PDF in browser (will trigger download)
            await OpenUrlInBrowserAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting stock on hand PDF: {ex.Message}");
            // TODO: Show error dialog
        }
    }

    private async Task ViewStockMovementAsync()
    {
        try
        {
            var queryParams = BuildStockMovementQueryParams();
            var url = $"{ApiConfig.BaseUrl}/stockreports/stock-movements{queryParams}";
            
            // Open in browser
            await OpenUrlInBrowserAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error viewing stock movement: {ex.Message}");
            // TODO: Show error dialog
        }
    }

    private async Task ExportStockMovementPdfAsync()
    {
        try
        {
            var queryParams = BuildStockMovementQueryParams();
            var url = $"{ApiConfig.BaseUrl}/stockreports/stock-movements/pdf{queryParams}";
            
            // Open PDF in browser (will trigger download)
            await OpenUrlInBrowserAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting stock movement PDF: {ex.Message}");
            // TODO: Show error dialog
        }
    }

    private async Task RecalculateStockAsync()
    {
        try
        {
            // Show confirmation dialog
            var confirmed = await ShowConfirmationAsync(
                "Recalculate Stock on Hand",
                "This will rebuild all stock levels from movements. Continue?");

            if (!confirmed)
                return;

            // Call API
            await _apiClient.PostAsync<object?, object>("stockreports/recalculate", null);

            // Show success message
            await ShowMessageAsync("Success", "Stock on hand has been recalculated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recalculating stock: {ex.Message}");
            await ShowMessageAsync("Error", $"Failed to recalculate stock: {ex.Message}");
        }
    }

    private async Task GenerateTicketReportAsync()
    {
        // TODO: Implement existing ticket report functionality
        await ShowMessageAsync("Info", "Ticket report generation coming soon!");
    }

    private string BuildStockMovementQueryParams()
    {
        var queryParts = new System.Collections.Generic.List<string>();

        if (StockMovementFromDate.HasValue)
            queryParts.Add($"fromDate={StockMovementFromDate.Value:yyyy-MM-ddTHH:mm:ss}");

        if (StockMovementToDate.HasValue)
            queryParts.Add($"toDate={StockMovementToDate.Value:yyyy-MM-ddTHH:mm:ss}");

        if (SelectedSite != null)
            queryParts.Add($"siteId={SelectedSite.SiteId}");

        if (SelectedProduct != null)
            queryParts.Add($"productId={SelectedProduct.ProductId}");

        return queryParts.Any() ? "?" + string.Join("&", queryParts) : "";
    }

    private async Task OpenUrlInBrowserAsync(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening URL: {ex.Message}");
            throw;
        }
        
        await Task.CompletedTask;
    }

    private async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        // TODO: Implement proper confirmation dialog
        // For now, return true (auto-confirm)
        Console.WriteLine($"Confirmation: {title} - {message}");
        return await Task.FromResult(true);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        // TODO: Implement proper message dialog
        Console.WriteLine($"Message: {title} - {message}");
        await Task.CompletedTask;
    }

    #endregion
}
