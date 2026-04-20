using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;
using Avalonia.VisualTree;
using System.Linq;

namespace MetalLink.Desktop.Views;

public partial class StockLevelsView : UserControl
{
    public StockLevelsView()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel mainVm)
            {
                mainVm.StockLevels.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(StockLevelsViewModel.HoveredProductId))
                    {
                        UpdateGridHoverState(mainVm.StockLevels.HoveredProductId);
                    }
                };
            }
        };

        var grid = this.FindControl<DataGrid>("StockGrid");
        if (grid != null)
        {
            grid.AddHandler(DataGrid.PointerEnteredEvent, (s, e) => 
            {
                if (e.Source is DataGridRow row && row.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto && DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.StockLevels.HoveredProductId = dto.ProductId;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);

            grid.AddHandler(DataGrid.PointerExitedEvent, (s, e) => 
            {
                if (DataContext is MainWindowViewModel mwvm)
                {
                    mwvm.StockLevels.HoveredProductId = null;
                }
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel | Avalonia.Interactivity.RoutingStrategies.Bubble);
        }
    }

    private void UpdateGridHoverState(int? hoveredId)
    {
        var grid = this.FindControl<DataGrid>("StockGrid");
        if (grid == null) return;

        foreach (var row in grid.GetVisualDescendants().OfType<DataGridRow>())
        {
            if (row.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto)
            {
                row.Classes.Set("is-hovered", dto.ProductId == hoveredId);
            }
        }
    }

    private void ToggleFilter(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.StockLevels.IsFilterExpanded = !vm.StockLevels.IsFilterExpanded;
    }

    private void ToggleChart(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.StockLevels.IsChartExpanded = !vm.StockLevels.IsChartExpanded;
    }

    private void ToggleResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.StockLevels.IsResultsExpanded = !vm.StockLevels.IsResultsExpanded;
    }

    private void OnMovementClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: MetalLink.Shared.Stock.StockLevelLookupDto row } && 
            DataContext is MainWindowViewModel mainVm)
        {
            mainVm.ShowStockMovementForProductCommand.Execute(row.ProductId);
        }
    }

    private void OnGridPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is MetalLink.Shared.Stock.StockLevelLookupDto dto && DataContext is MainWindowViewModel mwvm)
        {
            mwvm.StockLevels.HoveredProductId = dto.ProductId;
        }
    }

    private void OnGridPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel mwvm)
        {
            mwvm.StockLevels.HoveredProductId = null;
        }
    }
}
