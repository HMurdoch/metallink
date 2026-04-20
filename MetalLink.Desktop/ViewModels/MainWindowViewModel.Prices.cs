using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using MetalLink.Shared.Prices;
using MetalLink.Shared.Products;

namespace MetalLink.Desktop.ViewModels;

public partial class MainWindowViewModel
{
    // Master list for the current entity type
    private List<ProductPriceListDto> _allPricesTypePriceLists = new();

    // Commands
    public IAsyncRelayCommand LoadPricesCommand { get; private set; } = null!;
    public IRelayCommand ResetPricesFiltersCommand { get; private set; } = null!;
    public IRelayCommand ClearPricesList1Command { get; private set; } = null!;
    public IRelayCommand ClearPricesList2Command { get; private set; } = null!;
    public IRelayCommand ClearPricesList3Command { get; private set; } = null!;
    public IRelayCommand ClearPricesList4Command { get; private set; } = null!;

    partial void InitializePricesCommands()
    {
        LoadPricesCommand = new AsyncRelayCommand(ct => LoadPricesGridAsync(ct));
        ResetPricesFiltersCommand = new RelayCommand(ResetPricesFilters);
        ClearPricesList1Command = new RelayCommand(() => SelectedPricesList1 = null);
        ClearPricesList2Command = new RelayCommand(() => SelectedPricesList2 = null);
        ClearPricesList3Command = new RelayCommand(() => SelectedPricesList3 = null);
        ClearPricesList4Command = new RelayCommand(() => SelectedPricesList4 = null);

        PricesPagination.PageChanged += async (s, e) => await LoadPricesGridAsync();
    }

    // -------------------------------------------------------
    // Initialization (called when navigating to the section)
    // -------------------------------------------------------

    public async Task InitializePricesAsync()
    {
        PricesIsPriceListSelectionExpanded = true;
        PricesIsFilterExpanded = true;
        PricesIsResultsExpanded = true;

        if (PricesProductGroups.Count == 0)
            await LoadPricesProductGroupsAsync();

        await LoadPriceListsForEntityTypeAsync();
        await LoadPricesGridAsync();
    }

    // -------------------------------------------------------
    // Load product groups / letter filters for prices page
    // -------------------------------------------------------

    private async Task LoadPricesProductGroupsAsync(CancellationToken ct = default)
    {
        try
        {
            var groups = await _app.ProductsService.GetProductGroupsAsync(ct);
            PricesProductGroups.Clear();
            PricesProductGroups.Add(new ProductGroupDto { ProductGroupId = 0, ProductGroupName = "ALL" });
            foreach (var g in groups) PricesProductGroups.Add(g);
            SelectedPricesProductGroup = PricesProductGroups[0];

            // Load letter filters from starred products
            var result = await _app.ProductsService.LookupProductsAsync(null, 0, "ALL", false, 0, 1000, ct);
            PricesLetterFilters.Clear();
            PricesLetterFilters.Add("ALL");
            var letters = result.Items
                .Where(p => !string.IsNullOrEmpty(p.ProductName))
                .Select(p => char.ToUpperInvariant(p.ProductName[0]))
                .Distinct()
                .OrderBy(c => c);
            foreach (var c in letters) PricesLetterFilters.Add(c.ToString());
            SelectedPricesLetter = "ALL";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load product groups: {ex.Message}";
        }
    }

    // -------------------------------------------------------
    // Load price lists for current entity type
    // -------------------------------------------------------

    public async Task LoadPriceListsForEntityTypeAsync(CancellationToken ct = default)
    {
        try
        {
            var entity = PricesEntityType ?? "Customer";
            var lists = await _app.ApiClient.GetAsync<ProductPriceListDto[]>(
                $"api/product-price-lists?entityType={Uri.EscapeDataString(entity)}", ct);

            _allPricesTypePriceLists = (lists ?? Array.Empty<ProductPriceListDto>()).ToList();

            // Clear all selections (entity type changed)
            _selectedPricesList1 = null;
            _selectedPricesList2 = null;
            _selectedPricesList3 = null;
            _selectedPricesList4 = null;
            OnPropertyChanged(nameof(SelectedPricesList1));
            OnPropertyChanged(nameof(SelectedPricesList2));
            OnPropertyChanged(nameof(SelectedPricesList3));
            OnPropertyChanged(nameof(SelectedPricesList4));
            OnPropertyChanged(nameof(IsPriceColumn1Visible));
            OnPropertyChanged(nameof(IsPriceColumn2Visible));
            OnPropertyChanged(nameof(IsPriceColumn3Visible));
            OnPropertyChanged(nameof(IsPriceColumn4Visible));
            OnPropertyChanged(nameof(PriceColumn1Header));
            OnPropertyChanged(nameof(PriceColumn2Header));
            OnPropertyChanged(nameof(PriceColumn3Header));
            OnPropertyChanged(nameof(PriceColumn4Header));

            RefreshAvailablePriceLists();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load price lists: {ex.Message}";
        }
    }

    // -------------------------------------------------------
    // Refresh available items in each dropdown
    // (excludes selections from other dropdowns)
    // Uses incremental update (no Clear) to preserve ComboBox selection.
    // -------------------------------------------------------

    private void RefreshAvailablePriceLists()
    {
        static void Populate(
            System.Collections.ObjectModel.ObservableCollection<ProductPriceListDto> target,
            IEnumerable<ProductPriceListDto> all,
            HashSet<int> exclude)
        {
            var desired = all.Where(p => !exclude.Contains(p.ProductPriceListId)).ToList();
            var desiredIds = new HashSet<int>(desired.Select(d => d.ProductPriceListId));

            // Step 1: remove stale items (backwards to keep indices valid)
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!desiredIds.Contains(target[i].ProductPriceListId))
                    target.RemoveAt(i);
            }

            // Step 2: ensure items are present and in the correct order.
            // Positions 0..i-1 are already correct after each iteration.
            for (int i = 0; i < desired.Count; i++)
            {
                var id = desired[i].ProductPriceListId;

                if (i < target.Count && target[i].ProductPriceListId == id)
                    continue; // already in the right slot

                // Search from i onwards (0..i-1 are already settled)
                int found = -1;
                for (int j = i; j < target.Count; j++)
                {
                    if (target[j].ProductPriceListId == id) { found = j; break; }
                }

                if (found >= 0)
                    target.Move(found, i);   // exists at wrong position – move it
                else
                    target.Insert(i, desired[i]); // not present at all – insert
            }
        }

        var all = _allPricesTypePriceLists;

        Populate(PricesAvailableList1, all,
            new HashSet<int>(new[] { _selectedPricesList2, _selectedPricesList3, _selectedPricesList4 }
                .Where(x => x != null).Select(x => x!.ProductPriceListId)));

        Populate(PricesAvailableList2, all,
            new HashSet<int>(new[] { _selectedPricesList1, _selectedPricesList3, _selectedPricesList4 }
                .Where(x => x != null).Select(x => x!.ProductPriceListId)));

        Populate(PricesAvailableList3, all,
            new HashSet<int>(new[] { _selectedPricesList1, _selectedPricesList2, _selectedPricesList4 }
                .Where(x => x != null).Select(x => x!.ProductPriceListId)));

        Populate(PricesAvailableList4, all,
            new HashSet<int>(new[] { _selectedPricesList1, _selectedPricesList2, _selectedPricesList3 }
                .Where(x => x != null).Select(x => x!.ProductPriceListId)));
    }

    // -------------------------------------------------------
    // Reset product filters
    // -------------------------------------------------------

    private void ResetPricesFilters()
    {
        SelectedPricesProductGroup = PricesProductGroups.FirstOrDefault(g => g.ProductGroupId == 0);
        PricesSearchTerm = string.Empty;
        SelectedPricesLetter = "ALL";
    }

    // -------------------------------------------------------
    // Load the full prices grid
    // -------------------------------------------------------

    public async Task LoadPricesGridAsync(CancellationToken ct = default)
    {
        try
        {
            StatusMessage = "[STATUS] Loading prices...";

            int? gid = SelectedPricesProductGroup?.ProductGroupId;
            if (gid == 0) gid = null;

            // Load starred products with filters + pagination
            var productResult = await _app.ProductsService.LookupProductsAsync(
                PricesSearchTerm,
                gid,
                SelectedPricesLetter,
                false, // starred only
                PricesPagination.GetSkip(),
                PricesPagination.GetTake(),
                ct);

            PricesPagination.SetTotalRecords(productResult.TotalCount);

            // Load prices for selected price lists in parallel
            var pricesTasks = new (int slot, int? listId)[]
            {
                (1, _selectedPricesList1?.ProductPriceListId),
                (2, _selectedPricesList2?.ProductPriceListId),
                (3, _selectedPricesList3?.ProductPriceListId),
                (4, _selectedPricesList4?.ProductPriceListId),
            };

            var priceDicts = new Dictionary<int, decimal>[5]; // index 1-4 used
            for (int i = 1; i <= 4; i++) priceDicts[i] = new Dictionary<int, decimal>();

            await Task.WhenAll(pricesTasks
                .Where(t => t.listId.HasValue)
                .Select(async t =>
                {
                    try
                    {
                        var data = await _app.ApiClient.GetAsync<ProductPriceDto[]>(
                            $"api/product-price-lists/{t.listId}/prices", ct);
                        if (data != null)
                        {
                            lock (priceDicts)
                            {
                                foreach (var p in data)
                                    priceDicts[t.slot][p.ProductId] = p.Price;
                            }
                        }
                    }
                    catch { /* individual price list load failure is non-fatal */ }
                }));

            PricesResults.Clear();
            foreach (var product in productResult.Items)
            {
                var row = new PriceRowViewModel
                {
                    ProductId = product.ProductId,
                    HtsCode = product.HtsCode,
                    GroupName = product.ProductGroupName,
                    DisplayName = !string.IsNullOrWhiteSpace(product.StarredProductAlias)
                        ? product.StarredProductAlias
                        : (!string.IsNullOrWhiteSpace(product.ProductName)
                            ? product.ProductName
                            : product.ProductCode ?? string.Empty),
                    IsriCode = product.ProductCode,
                };

                // Use SetPrice so PriceChanged event is NOT triggered on initial load
                row.SetPrice(1, priceDicts[1].GetValueOrDefault(product.ProductId, 0m));
                row.SetPrice(2, priceDicts[2].GetValueOrDefault(product.ProductId, 0m));
                row.SetPrice(3, priceDicts[3].GetValueOrDefault(product.ProductId, 0m));
                row.SetPrice(4, priceDicts[4].GetValueOrDefault(product.ProductId, 0m));

                // Wire AFTER initial values – user edits fire PriceChanged
                row.PriceChanged += async (productId, slot, price) =>
                    await SavePriceAsync(productId, slot, price);

                PricesResults.Add(row);
            }

            // Set price list names so tooltip value-lines are correct immediately
            ApplyPriceListNamesToRows();

            StatusMessage = $"[STATUS] Loaded {PricesResults.Count} products.";

            // Load tooltip (stock + transactions) in the background – non-blocking
            _ = LoadTooltipDataAsync(PricesResults.ToList(), ct);
        }
        catch (OperationCanceledException) { /* navigation away */ }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load prices: {ex.Message}";
        }
    }

    // -------------------------------------------------------
    // Refresh a single price column (slot 1-4) without
    // rebuilding the whole grid
    // -------------------------------------------------------

    private async Task LoadPriceColumnAsync(int slot, CancellationToken ct = default)
    {
        var priceList = slot switch
        {
            1 => _selectedPricesList1,
            2 => _selectedPricesList2,
            3 => _selectedPricesList3,
            4 => _selectedPricesList4,
            _ => null
        };

        if (priceList == null)
        {
            // Price list deselected – clear this column's values
            foreach (var row in PricesResults)
                row.SetPrice(slot, 0m);
            ApplyPriceListNamesToRows();
            return;
        }

        try
        {
            var data = await _app.ApiClient.GetAsync<ProductPriceDto[]>(
                $"api/product-price-lists/{priceList.ProductPriceListId}/prices", ct);

            var dict = data?.ToDictionary(p => p.ProductId, p => p.Price)
                       ?? new Dictionary<int, decimal>();

            foreach (var row in PricesResults)
                row.SetPrice(slot, dict.GetValueOrDefault(row.ProductId, 0m));

            ApplyPriceListNamesToRows();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Failed to load prices for column {slot}: {ex.Message}";
        }
    }

    // -------------------------------------------------------
    // Save a single price change to the API
    // -------------------------------------------------------

    public async Task SavePriceAsync(int productId, int slot, decimal price)
    {
        var priceListId = slot switch
        {
            1 => _selectedPricesList1?.ProductPriceListId,
            2 => _selectedPricesList2?.ProductPriceListId,
            3 => _selectedPricesList3?.ProductPriceListId,
            4 => _selectedPricesList4?.ProductPriceListId,
            _ => (int?)null
        };

        if (priceListId == null) return;

        try
        {
            await _app.ProductsService.SetProductPriceAsync(productId, priceListId.Value, price);
            StatusMessage = $"[STATUS] Price saved: {price:F2}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[STATUS] Save price failed: {ex.Message}";
        }
    }

    // -------------------------------------------------------
    // Tooltip: price list names + stock/transaction data
    // -------------------------------------------------------

    private void ApplyPriceListNamesToRows()
    {
        foreach (var row in PricesResults)
        {
            row.PriceListName1 = _selectedPricesList1?.ProductPriceListName;
            row.PriceListName2 = _selectedPricesList2?.ProductPriceListName;
            row.PriceListName3 = _selectedPricesList3?.ProductPriceListName;
            row.PriceListName4 = _selectedPricesList4?.ProductPriceListName;
        }
    }

    private async Task LoadTooltipDataAsync(List<PriceRowViewModel> rows, CancellationToken ct = default)
    {
        if (rows.Count == 0) return;
        try
        {
            var productIds = rows.Select(r => r.ProductId).ToArray();
            var data = await _app.ApiClient.PostAsync<int[], Dictionary<int, ProductPriceTooltipDto>>(
                "api/products/stock-tooltips", productIds, ct);
            if (data == null) return;

            foreach (var row in rows)
            {
                if (!data.TryGetValue(row.ProductId, out var tooltip)) continue;
                var transactions = tooltip.LastTransactions.Select(t => new PriceTransactionItemViewModel
                {
                    IsBuy      = t.IsBuy,
                    Date       = t.Date,
                    QuantityKg = t.QuantityKg
                });
                row.SetTooltipData(tooltip.StockOnHandKg, transactions);
            }
        }
        catch
        {
            // Tooltip data is non-critical; swallow silently
        }
    }
}
