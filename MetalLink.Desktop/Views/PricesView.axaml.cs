using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class PricesView : UserControl
{
    private DataGrid? _pricesDataGrid;
    private MainWindowViewModel? _lastVm;

    // Price column indices in the DataGrid.Columns collection
    // Columns: [0] HTS Code, [1] Group, [2] ISRI Name/Alias, [3] ISRI Code, [4-7] Price 1-4
    private const int PriceColStartIndex = 4;
    private const int PriceColCount = 4;

    // Tracks the TextBox, slot (1-4), and row VM for the cell currently in edit mode.
    // Used by OnCellEditEnded to save exactly once on Commit.
    private TextBox? _editingTextBox;
    private int _editingSlot;
    private PriceRowViewModel? _editingRowVm;
    // Captures every keystroke via TextChanged; more reliable than reading Text in CellEditEnded
    // because Avalonia may reset the OneWay binding before the event fires.
    private string? _editingRawText;

    public PricesView()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        DataContextChanged += (s, e) =>
        {
            if (_lastVm != null)
                _lastVm.PropertyChanged -= OnVmPropertyChanged;

            _lastVm = DataContext as MainWindowViewModel;

            if (_lastVm != null)
            {
                _lastVm.PropertyChanged += OnVmPropertyChanged;
                UpdateColumnVisibilityAndHeaders(_lastVm);
            }
        };

        // Wire DataGrid UX after AXAML is loaded
        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        var grid = GetPricesGrid();
        if (grid == null) return;

        grid.PreparingCellForEdit += OnPreparingCellForEdit;
        grid.BeginningEdit += (_, __) => _isEditingCell = true;
        grid.CellEditEnded += OnCellEditEnded;
        grid.KeyDown += OnGridKeyDown;
        grid.LoadingRow += OnGridLoadingRow;
    }

    private static void OnGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        var tooltipView = new PriceRowTooltipView();
        ToolTip.SetTip(e.Row, tooltipView);
        ToolTip.SetShowDelay(e.Row, 600);

        void UpdateDataContext()
        {
            if (e.Row.DataContext is PriceRowViewModel vm)
                tooltipView.DataContext = vm;
        }

        UpdateDataContext();
        e.Row.DataContextChanged += (_, _) => UpdateDataContext();
    }

    private bool _isEditingCell;

    // Select-all text when a cell enters edit mode; also tracks which cell is being edited.
    private void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        // For DataGridTextColumn, EditingElement is the TextBox.
        // For DataGridTemplateColumn, EditingElement is the DataTemplate root (may wrap a TextBox).
        TextBox? tb = e.EditingElement as TextBox
            ?? e.EditingElement?.FindDescendantOfType<TextBox>();
        if (tb == null) return;

        // Unsubscribe from any previous editing TextBox
        if (_editingTextBox != null)
            _editingTextBox.TextChanged -= OnEditingTextChanged;

        // Determine which price slot this column maps to (1-4); 0 means a non-price column.
        var colIndex = GetPricesGrid()?.Columns.IndexOf(e.Column) ?? -1;
        _editingSlot    = colIndex >= PriceColStartIndex ? colIndex - PriceColStartIndex + 1 : 0;
        _editingTextBox = tb;
        _editingRowVm   = e.Row.DataContext as PriceRowViewModel;
        _editingRawText = null; // will be set by TextChanged once user starts typing

        // Track every keystroke: safer than reading Text in CellEditEnded where the
        // DataGrid may have already re-applied the OneWay binding value.
        tb.TextChanged += OnEditingTextChanged;

        Dispatcher.UIThread.Post(() => { tb.SelectAll(); }, DispatcherPriority.Background);
    }

    private void OnEditingTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
            _editingRawText = tb.Text;
    }

    // Fires when a cell edit ends (Commit or Cancel).
    // Saves the typed value exactly once on Commit; does nothing on Cancel.
    private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
    {
        _isEditingCell = false;

        if (_editingTextBox != null)
            _editingTextBox.TextChanged -= OnEditingTextChanged;

        if (e.EditAction == DataGridEditAction.Commit
            && _editingRowVm != null
            && _editingSlot  > 0)
        {
            // Prefer the value tracked via TextChanged (captured before DataGrid may reset the binding);
            // fall back to reading Text directly in case the user committed without typing.
            var text = (_editingRawText ?? _editingTextBox?.Text ?? string.Empty).Trim();

            // Try current culture first (handles localised decimal separators), then invariant.
            if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture,  out var newPrice)
             && !decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out newPrice))
            {
                newPrice = 0m;
            }

            // Setting via property setter (not SetPrice) fires PriceChanged → SavePriceAsync.
            switch (_editingSlot)
            {
                case 1: _editingRowVm.Price1 = newPrice; break;
                case 2: _editingRowVm.Price2 = newPrice; break;
                case 3: _editingRowVm.Price3 = newPrice; break;
                case 4: _editingRowVm.Price4 = newPrice; break;
            }
        }

        _editingTextBox = null;
        _editingRowVm   = null;
        _editingSlot    = 0;
        _editingRawText = null;
    }

    // Keyboard behaviour inside editing cells
    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        var grid = GetPricesGrid();
        if (grid == null || !_isEditingCell) return;

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            var col = grid.CurrentColumn; // capture before CommitEdit may clear it
            grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
            MoveToAdjacentRow(grid, delta: 1, col);
        }
        else if (e.Key == Key.Tab)
        {
            e.Handled = true;
            grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
            int shift = (e.KeyModifiers & KeyModifiers.Shift) != 0 ? -1 : 1;
            MoveToAdjacentPriceColumn(grid, shift);
        }
        else if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right)
        {
            e.Handled = true;
            grid.CancelEdit(DataGridEditingUnit.Cell);
        }
    }

    /// <summary>Moves the current cell to the row at <paramref name="delta"/> offset, same column.</summary>
    private static void MoveToAdjacentRow(DataGrid grid, int delta, DataGridColumn? col = null)
    {
        col ??= grid.CurrentColumn;
        var items = grid.ItemsSource as System.Collections.IList;
        if (col == null || items == null) return;

        var currentItem = grid.SelectedItem;
        if (currentItem == null) return;

        var currentIndex = items.IndexOf(currentItem);
        var targetIndex = currentIndex + delta;
        if (targetIndex < 0 || targetIndex >= items.Count) return;

        var targetItem = items[targetIndex];
        grid.SelectedItem = targetItem;
        grid.ScrollIntoView(targetItem, col);
        // Dispatch CurrentColumn + BeginEdit together after the grid has processed the selection
        // change and scroll layout; setting CurrentColumn here could be overwritten by the DataGrid.
        Dispatcher.UIThread.Post(() =>
        {
            grid.CurrentColumn = col;
            grid.BeginEdit();
        }, DispatcherPriority.Background);
    }

    /// <summary>Moves the current cell to the next/previous visible price column, same row.</summary>
    private static void MoveToAdjacentPriceColumn(DataGrid grid, int shift)
    {
        if (grid.CurrentColumn == null) return;

        var visiblePriceCols = grid.Columns
            .Skip(PriceColStartIndex)
            .Take(PriceColCount)
            .Where(c => c.IsVisible)
            .ToList();

        if (visiblePriceCols.Count == 0) return;

        var current = visiblePriceCols.IndexOf(grid.CurrentColumn);
        if (current < 0) return;

        var nextIndex = current + shift;
        if (nextIndex < 0 || nextIndex >= visiblePriceCols.Count) return;

        var nextCol = visiblePriceCols[nextIndex];
        grid.CurrentColumn = nextCol;
        // Dispatch BeginEdit so the grid finishes updating its column state first
        Dispatcher.UIThread.Post(() => grid.BeginEdit(), DispatcherPriority.Background);
    }

    // -------------------------------------------------------
    // Column visibility / header updates driven by VM changes
    // -------------------------------------------------------

    private DataGrid? GetPricesGrid()
        => _pricesDataGrid ??= this.FindControl<DataGrid>("PricesDataGrid");

    private void UpdateColumnVisibilityAndHeaders(MainWindowViewModel vm)
    {
        var grid = GetPricesGrid();
        if (grid == null || grid.Columns.Count < PriceColStartIndex + PriceColCount) return;


        grid.Columns[4].IsVisible = vm.IsPriceColumn1Visible;
        grid.Columns[4].Header   = vm.PriceColumn1Header;

        grid.Columns[5].IsVisible = vm.IsPriceColumn2Visible;
        grid.Columns[5].Header   = vm.PriceColumn2Header;

        grid.Columns[6].IsVisible = vm.IsPriceColumn3Visible;
        grid.Columns[6].Header   = vm.PriceColumn3Header;

        grid.Columns[7].IsVisible = vm.IsPriceColumn4Visible;
        grid.Columns[7].Header   = vm.PriceColumn4Header;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        switch (e.PropertyName)
        {
            case nameof(vm.IsPriceColumn1Visible):
            case nameof(vm.IsPriceColumn2Visible):
            case nameof(vm.IsPriceColumn3Visible):
            case nameof(vm.IsPriceColumn4Visible):
            case nameof(vm.PriceColumn1Header):
            case nameof(vm.PriceColumn2Header):
            case nameof(vm.PriceColumn3Header):
            case nameof(vm.PriceColumn4Header):
                UpdateColumnVisibilityAndHeaders(vm);
                break;
        }
    }

    // -------------------------------------------------------
    // Collapsible panel toggles
    // -------------------------------------------------------

    private void TogglePriceListSelection(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PricesIsPriceListSelectionExpanded = !vm.PricesIsPriceListSelectionExpanded;
    }

    private void ToggleFilters(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PricesIsFilterExpanded = !vm.PricesIsFilterExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.PricesIsResultsExpanded = !vm.PricesIsResultsExpanded;
    }
}

