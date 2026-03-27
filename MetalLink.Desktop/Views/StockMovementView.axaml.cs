using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;
using Avalonia.VisualTree;
using System.Linq;

namespace MetalLink.Desktop.Views;

public partial class StockMovementView : UserControl
{
    public StockMovementView()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel mainVm)
            {
                mainVm.StockMovement.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(StockMovementViewModel.HoveredProductId))
                    {
                        UpdateHoverState(mainVm.StockMovement.HoveredProductId);
                    }
                };
            }
        };

        var grid = this.FindControl<DataGrid>("MovementGrid");
        if (grid != null)
        {
            grid.AddHandler(DataGrid.PointerEnteredEvent, (s, e) => 
            {
                if (e.Source is DataGridRow row && row.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto && DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.StockMovement.HoveredProductId = dto.ProductId;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);

            grid.AddHandler(DataGrid.PointerExitedEvent, (s, e) => 
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.StockMovement.HoveredProductId = null;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);

            // Handle manual deselection on click
            grid.PointerPressed += (s, e) => {
                if (e.Source is DataGridRow row && row.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto && DataContext is MainWindowViewModel mwvm)
                {
                    if (mwvm.StockMovement.SelectedProductRow == dto)
                    {
                        mwvm.StockMovement.SelectedProductRow = null;
                        e.Handled = true;
                    }
                }
            };
        }

        var tlist = this.FindControl<DataGrid>("TransactionsList");
        if (tlist != null)
        {
            tlist.AddHandler(DataGrid.PointerEnteredEvent, (s, e) => 
            {
                if (e.Source is DataGridRow row && row.DataContext is StockMovementViewModel.TransactionViewModel t && DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.StockMovement.HoveredProductId = t.ProductId;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);

            tlist.AddHandler(DataGrid.PointerExitedEvent, (s, e) => 
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.StockMovement.HoveredProductId = null;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        }
    }

    private void ToggleFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.StockMovement.IsFilterExpanded = !vm.StockMovement.IsFilterExpanded;
    }

    private void ToggleChart(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.StockMovement.IsChartExpanded = !vm.StockMovement.IsChartExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.StockMovement.IsResultsExpanded = !vm.StockMovement.IsResultsExpanded;
    }

    private void OnGridPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control c && DataContext is MainWindowViewModel mwvm)
        {
            if (c.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto)
                mwvm.StockMovement.HoveredProductId = dto.ProductId;
            else if (c.DataContext is StockMovementViewModel.TransactionViewModel t)
                mwvm.StockMovement.HoveredProductId = t.ProductId;
        }
    }

    private void OnGridPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel mwvm)
        {
            mwvm.StockMovement.HoveredProductId = null;
        }
    }

    private void OnChartPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is LiveChartsCore.SkiaSharpView.Avalonia.CartesianChart chart && DataContext is MainWindowViewModel mwvm)
        {
            var p = e.GetPosition(chart);
            var points = chart.GetPointsAt(new LiveChartsCore.Drawing.LvcPointD(p.X, p.Y));
            var closest = points.FirstOrDefault();
            if (closest != null && closest.Context.Series.Name != null)
            {
                // Series name is usually product name or "{productName} Transactions"
                var name = closest.Context.Series.Name;
                if (name.EndsWith(" Transactions")) name = name.Substring(0, name.Length - 13);
                
                var prod = mwvm.StockMovement.Rows.FirstOrDefault(r => r.ProductName == name);
                if (prod != null)
                {
                    mwvm.StockMovement.HoveredProductId = prod.ProductId;
                    return;
                }
            }
            mwvm.StockMovement.HoveredProductId = null;
        }
    }

    private void OnChartPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel mwvm)
        {
            mwvm.StockMovement.HoveredProductId = null;
        }
    }

    private void OnChartPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Passthrough scroll to the scrollviewer
        var scrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
        if (scrollViewer != null)
        {
            var currentOffset = scrollViewer.Offset;
            var delta = e.Delta.Y * 50; // Adjust multiplier for sensitivity
            scrollViewer.Offset = new Avalonia.Vector(currentOffset.X, currentOffset.Y - delta);
            e.Handled = true;
        }
    }

    private void UpdateHoverState(int? hoveredId)
    {
        var grid = this.FindControl<DataGrid>("MovementGrid");
        if (grid != null)
        {
            foreach (var row in grid.GetVisualDescendants().OfType<DataGridRow>())
            {
                if (row.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto)
                {
                    row.Classes.Set("is-hovered", dto.ProductId == hoveredId);
                }
            }
        }

        var tlist = this.FindControl<DataGrid>("TransactionsList");
        if (tlist != null)
        {
            foreach (var row in tlist.GetVisualDescendants().OfType<DataGridRow>())
            {
                if (row.DataContext is StockMovementViewModel.TransactionViewModel t)
                {
                    row.Classes.Set("is-hovered", t.ProductId == hoveredId);
                }
            }
        }
    }
}
