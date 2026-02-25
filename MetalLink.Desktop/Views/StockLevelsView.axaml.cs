using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using MetalLink.Desktop.ViewModels;
using MetalLink.Shared.Stock;

namespace MetalLink.Desktop.Views;

public partial class StockLevelsView : UserControl
{
    private DataGrid? _grid;

    public StockLevelsView()
    {
        InitializeComponent();

        _grid = this.FindControl<DataGrid>("StockGrid");

        // Bind VM if not already provided
        if (DataContext == null && Application.Current is MetalLink.Desktop.App app)
        {
            DataContext = new StockLevelsViewModel(app.ApiClient);
        }

        // When chart hover changes, select the matching row for emphasis
        if (DataContext is StockLevelsViewModel vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(StockLevelsViewModel.HoveredProductId))
                {
                    if (_grid is null) return;
                    if (vm.HoveredProductId is null) return;

                    var match = vm.StockRows.FirstOrDefault(r => r.ProductId == vm.HoveredProductId.Value);
                    if (match is not null)
                        _grid.SelectedItem = match;
                }
            };
        }
    }

    // When user sorts the grid, reorder the backing collection so the chart uses the same order.
    private string? _sortColumn;
    private bool _sortAsc = true;

    private void OnStockGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (DataContext is not StockLevelsViewModel vm)
            return;

        _grid ??= sender as DataGrid;
        if (_grid is null)
            return;

        var header = e.Column.Header?.ToString() ?? string.Empty;

        // Toggle sort direction if same column; otherwise default ascending.
        if (string.Equals(_sortColumn, header, StringComparison.Ordinal))
            _sortAsc = !_sortAsc;
        else
        {
            _sortColumn = header;
            _sortAsc = true;
        }

        IOrderedEnumerable<StockLevelLookupDto> ordered = header switch
        {
            "Code" => _sortAsc ? vm.StockRows.OrderBy(r => r.ProductCode) : vm.StockRows.OrderByDescending(r => r.ProductCode),
            "Product" => _sortAsc ? vm.StockRows.OrderBy(r => r.ProductName) : vm.StockRows.OrderByDescending(r => r.ProductName),
            _ => _sortAsc ? vm.StockRows.OrderBy(r => r.WeightKg) : vm.StockRows.OrderByDescending(r => r.WeightKg)
        };

        // Reorder backing collection so chart stays in sync with grid.
        vm.SortAll(header, _sortAsc);
    }

    // Hovering grid rows updates chart hover
    private void OnStockGridPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not StockLevelsViewModel vm)
            return;

        var src = e.Source as Control;
        var row = src?.GetVisualAncestors().OfType<DataGridRow>().FirstOrDefault();
        if (row?.DataContext is StockLevelLookupDto dto)
        {
            vm.HoveredProductId = dto.ProductId;
        }
    }

    private void OnStockGridPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is StockLevelsViewModel vm)
            vm.HoveredProductId = null;
    }
}
