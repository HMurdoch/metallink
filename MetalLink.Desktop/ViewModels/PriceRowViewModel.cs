using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MetalLink.Desktop.ViewModels;

/// <summary>
/// Represents one row in the Prices datagrid.
/// Price1-4 map to the four optionally-selected price lists.
/// Use <see cref="SetPrice"/> for programmatic updates (no save triggered).
/// Assigning via the public property setters fires <see cref="PriceChanged"/> which auto-saves.
/// </summary>
public class PriceRowViewModel : INotifyPropertyChanged
{
    public int ProductId { get; set; }
    public string? HtsCode { get; set; }
    public string? GroupName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? IsriCode { get; set; }

    // --------------------------------------------------------
    // Price slots – property setters fire PriceChanged (save)
    // --------------------------------------------------------

    private decimal _price1;
    public decimal Price1
    {
        get => _price1;
        set
        {
            if (_price1 == value) return;
            _price1 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockValueLine1));
            OnPropertyChanged(nameof(IsStockValueLine1Visible));
            PriceChanged?.Invoke(ProductId, 1, value);
        }
    }

    private decimal _price2;
    public decimal Price2
    {
        get => _price2;
        set
        {
            if (_price2 == value) return;
            _price2 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockValueLine2));
            OnPropertyChanged(nameof(IsStockValueLine2Visible));
            PriceChanged?.Invoke(ProductId, 2, value);
        }
    }

    private decimal _price3;
    public decimal Price3
    {
        get => _price3;
        set
        {
            if (_price3 == value) return;
            _price3 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockValueLine3));
            OnPropertyChanged(nameof(IsStockValueLine3Visible));
            PriceChanged?.Invoke(ProductId, 3, value);
        }
    }

    private decimal _price4;
    public decimal Price4
    {
        get => _price4;
        set
        {
            if (_price4 == value) return;
            _price4 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockValueLine4));
            OnPropertyChanged(nameof(IsStockValueLine4Visible));
            PriceChanged?.Invoke(ProductId, 4, value);
        }
    }

    /// <summary>
    /// Sets the backing field directly (no save triggered) and notifies the UI.
    /// Use this for programmatic / initial load updates.
    /// </summary>
    public void SetPrice(int slot, decimal value)
    {
        switch (slot)
        {
            case 1: _price1 = value; OnPropertyChanged(nameof(Price1)); break;
            case 2: _price2 = value; OnPropertyChanged(nameof(Price2)); break;
            case 3: _price3 = value; OnPropertyChanged(nameof(Price3)); break;
            case 4: _price4 = value; OnPropertyChanged(nameof(Price4)); break;
        }
    }

    /// <summary>
    /// Fired when the user edits a price (after cell-edit commit via DataGrid binding).
    /// Parameters: (productId, slot 1-4, newPrice)
    /// </summary>
    public event Action<int, int, decimal>? PriceChanged;

    // --------------------------------------------------------
    // Tooltip data – loaded asynchronously after grid builds
    // --------------------------------------------------------

    private decimal _stockOnHandKg;
    public decimal StockOnHandKg
    {
        get => _stockOnHandKg;
        private set
        {
            _stockOnHandKg = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StockSummaryLine));
            OnPropertyChanged(nameof(StockValueLine1));
            OnPropertyChanged(nameof(StockValueLine2));
            OnPropertyChanged(nameof(StockValueLine3));
            OnPropertyChanged(nameof(StockValueLine4));
            OnPropertyChanged(nameof(IsStockValueLine1Visible));
            OnPropertyChanged(nameof(IsStockValueLine2Visible));
            OnPropertyChanged(nameof(IsStockValueLine3Visible));
            OnPropertyChanged(nameof(IsStockValueLine4Visible));
        }
    }

    // These are set by the main VM when price list selections change
    private string? _priceListName1;
    public string? PriceListName1
    {
        get => _priceListName1;
        set { _priceListName1 = value; OnPropertyChanged(nameof(StockValueLine1)); OnPropertyChanged(nameof(IsStockValueLine1Visible)); }
    }

    private string? _priceListName2;
    public string? PriceListName2
    {
        get => _priceListName2;
        set { _priceListName2 = value; OnPropertyChanged(nameof(StockValueLine2)); OnPropertyChanged(nameof(IsStockValueLine2Visible)); }
    }

    private string? _priceListName3;
    public string? PriceListName3
    {
        get => _priceListName3;
        set { _priceListName3 = value; OnPropertyChanged(nameof(StockValueLine3)); OnPropertyChanged(nameof(IsStockValueLine3Visible)); }
    }

    private string? _priceListName4;
    public string? PriceListName4
    {
        get => _priceListName4;
        set { _priceListName4 = value; OnPropertyChanged(nameof(StockValueLine4)); OnPropertyChanged(nameof(IsStockValueLine4Visible)); }
    }

    private List<PriceTransactionItemViewModel> _recentTransactions = new();
    public List<PriceTransactionItemViewModel> RecentTransactions
    {
        get => _recentTransactions;
        private set { _recentTransactions = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoTransactions)); }
    }

    public string StockSummaryLine => $"Stock on Hand: {StockOnHandKg:N2} kg";

    public string? StockValueLine1 => _priceListName1 != null
        ? $"  \u2192 {_priceListName1}: R {StockOnHandKg * _price1:N2}"
        : null;
    public string? StockValueLine2 => _priceListName2 != null
        ? $"  \u2192 {_priceListName2}: R {StockOnHandKg * _price2:N2}"
        : null;
    public string? StockValueLine3 => _priceListName3 != null
        ? $"  \u2192 {_priceListName3}: R {StockOnHandKg * _price3:N2}"
        : null;
    public string? StockValueLine4 => _priceListName4 != null
        ? $"  \u2192 {_priceListName4}: R {StockOnHandKg * _price4:N2}"
        : null;

    public bool IsStockValueLine1Visible => StockValueLine1 != null;
    public bool IsStockValueLine2Visible => StockValueLine2 != null;
    public bool IsStockValueLine3Visible => StockValueLine3 != null;
    public bool IsStockValueLine4Visible => StockValueLine4 != null;
    public bool HasNoTransactions        => _recentTransactions.Count == 0;

    /// <summary>Called after async tooltip data arrives from the API.</summary>
    public void SetTooltipData(decimal stockOnHandKg, IEnumerable<PriceTransactionItemViewModel> transactions)
    {
        _recentTransactions = new List<PriceTransactionItemViewModel>(transactions);
        StockOnHandKg = stockOnHandKg; // fires all StockValueLine notifications too
        OnPropertyChanged(nameof(RecentTransactions));
        OnPropertyChanged(nameof(HasNoTransactions));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
