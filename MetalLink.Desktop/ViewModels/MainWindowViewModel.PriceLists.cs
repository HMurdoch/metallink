using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Prices;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    public static Func<char, string> EntityFlagConverter => flag => flag == 'C' ? "Customer" : "Buyer";

    public IAsyncRelayCommand SearchPriceListsCommand { get; private set; } = null!;
    public IAsyncRelayCommand CreatePriceListCommand { get; private set; } = null!;
    public IAsyncRelayCommand UpdatePriceListCommand { get; private set; } = null!;
    public IRelayCommand ClearPriceListFormCommand { get; private set; } = null!;
    public IRelayCommand ResetPriceListFiltersCommand { get; private set; } = null!;
    public IAsyncRelayCommand<ProductPriceListDto> DeletePriceListCommand { get; private set; } = null!;

    partial void InitializePriceListsCommands()
    {
        SearchPriceListsCommand = new AsyncRelayCommand(ct => SearchPriceListsAsync(ct));
        CreatePriceListCommand = new AsyncRelayCommand(ct => CreatePriceListAsync(ct));
        UpdatePriceListCommand = new AsyncRelayCommand(ct => UpdatePriceListAsync(ct));
        ClearPriceListFormCommand = new RelayCommand(ClearPriceListForm);
        ResetPriceListFiltersCommand = new RelayCommand(ResetPriceListFilters);
        DeletePriceListCommand = new AsyncRelayCommand<ProductPriceListDto>(DeletePriceListAsync);
    }

    private void ResetPriceListFilters()
    {
        _priceListSearchTerm = string.Empty;
        OnPropertyChanged(nameof(PriceListSearchTerm));
        _selectedPriceListEntityType = "Customer";
        OnPropertyChanged(nameof(SelectedPriceListEntityType));
        _ = SearchPriceListsAsync();
    }

    public async Task DeletePriceListAsync(ProductPriceListDto? list)
    {
        if (list == null) return;
        var ok = await ConfirmAsync($"Are you sure you want to delete price list '{list.ProductPriceListName}'?");
        if (!ok) return;

        try
        {
            StatusMessage = $"[STATUS] Deleting price list '{list.ProductPriceListName}'...";
            list.IsActive = false;
            await _app.ApiClient.PutAsJsonAsync($"api/product-price-lists/{list.ProductPriceListId}", list);
            StatusMessage = $"[STATUS] Price list '{list.ProductPriceListName}' deleted.";
            await SearchPriceListsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Delete failed: {ex.Message}";
        }
    }

    public async Task SearchPriceListsAsync(CancellationToken ct = default)
    {
        try
        {
            StatusMessage = "[STATUS] Searching price lists...";
            var term = PriceListSearchTerm?.Trim() ?? "";
            var entity = SelectedPriceListEntityType ?? "Customer";
            var results = await _app.ApiClient.GetAsync<ProductPriceListDto[]>($"api/product-price-lists?term={Uri.EscapeDataString(term)}&entityType={Uri.EscapeDataString(entity)}", ct);
            
            PriceListsResults.Clear();
            if (results != null)
            {
                foreach (var r in results) PriceListsResults.Add(r);
            }
            StatusMessage = $"[STATUS] Found {PriceListsResults.Count} price lists.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnEditPriceList(ProductPriceListDto list)
    {
        EditingPriceListId = list.ProductPriceListId;
        PriceListName = list.ProductPriceListName ?? string.Empty;
        PriceListDescription = list.ProductPriceListDescription;
        PriceListEntityType = list.EntityFlag == 'C' ? "Customer" : "Buyer";
        PriceListsIsCreateEditExpanded = true;
        
        // Ensure form reflects the selected values
        OnPropertyChanged(nameof(PriceListEntityType));
        OnPropertyChanged(nameof(PriceListName));
        OnPropertyChanged(nameof(PriceListDescription));
        
        OnPropertyChanged(nameof(IsPriceListEditMode));
        OnPropertyChanged(nameof(IsPriceListCreateMode));
        OnPropertyChanged(nameof(PriceListSaveButtonText));
    }

    private void ClearPriceListForm()
    {
        EditingPriceListId = null;
        PriceListName = string.Empty;
        PriceListDescription = string.Empty;
        PriceListEntityType = "Customer";
        SelectedPriceList = null;
        
        // Ensure form reflects the selected values
        OnPropertyChanged(nameof(PriceListEntityType));
        OnPropertyChanged(nameof(PriceListName));
        OnPropertyChanged(nameof(PriceListDescription));

        OnPropertyChanged(nameof(IsPriceListEditMode));
        OnPropertyChanged(nameof(IsPriceListCreateMode));
        OnPropertyChanged(nameof(PriceListSaveButtonText));
    }

    public async Task CreatePriceListAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            StatusMessage = $"[STATUS] Creating price list '{PriceListName}'...";
            var dto = new ProductPriceListDto
            {
                ProductPriceListName = PriceListName,
                ProductPriceListDescription = PriceListDescription,
                EntityFlag = PriceListEntityType == "Customer" ? 'C' : 'B',
                IsActive = true
            };
            
            await _app.ApiClient.PostAsJsonAsync("api/product-price-lists", dto, ct);
            StatusMessage = $"[STATUS] Price list '{PriceListName}' created successfully.";
            ClearPriceListForm();
            await SearchPriceListsAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Create failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task UpdatePriceListAsync(CancellationToken ct = default)
    {
        if (IsBusy || !EditingPriceListId.HasValue) return;
        IsBusy = true;
        try
        {
            StatusMessage = $"[STATUS] Updating price list '{PriceListName}'...";
            var dto = new ProductPriceListDto
            {
                ProductPriceListId = EditingPriceListId.Value,
                ProductPriceListName = PriceListName,
                ProductPriceListDescription = PriceListDescription,
                EntityFlag = PriceListEntityType == "Customer" ? 'C' : 'B',
                IsActive = true
            };
            
            await _app.ApiClient.PutAsJsonAsync($"api/product-price-lists/{EditingPriceListId}", dto, ct);
            StatusMessage = $"[STATUS] Price list '{PriceListName}' updated successfully.";
            ClearPriceListForm();
            await SearchPriceListsAsync(ct);
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Update failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
