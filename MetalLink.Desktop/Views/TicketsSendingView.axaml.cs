using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MetalLink.Desktop.ViewModels.Sending;
using System;

namespace MetalLink.Desktop.Views;

public partial class TicketsSendingView : UserControl
{
    private ScrollViewer? _scrollViewer;
    private Border? _addProductLinesSection;

    public TicketsSendingView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _scrollViewer = this.FindControl<ScrollViewer>("TicketsScrollViewer");
        _addProductLinesSection = this.FindControl<Border>("AddProductLinesSection");
    }

    public void ScrollToAddProductLines()
    {
        if (_scrollViewer != null && _addProductLinesSection != null)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Calculate the offset to scroll to the Add Product Lines section
                var offset = _addProductLinesSection.Bounds.Y;
                _scrollViewer.Offset = new Avalonia.Vector(0, offset);
            });
        }
    }

    private async void TareTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MetalLink.Desktop.ViewModels.Sending.TicketsSendingViewModel vm) return;
        if (sender is not TextBox tb) return;
        if (tb.DataContext is not MetalLink.Desktop.ViewModels.Sending.TicketsSendingViewModel.SendingLineRow row) return;

        if (!decimal.TryParse(tb.Text?.Replace(',', '.').Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tare))
            return;

        if (!row.IsEditable) return;
        await vm.UpdateLastLineTareAsync(row.TicketSendingLineId, tare);
    }

    private void TareTextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // DataGrid cell editors can steal selection on focus.
            // Defer selection to ensure the caret/selection is applied after the template finishes focusing.
            Dispatcher.UIThread.Post(textBox.SelectAll);
        }
    }

    private void SendingTicketsGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Prevent automatic scrolling when selecting different rows
        // This avoids the annoying "jump" behavior when clicking on different tickets
        e.Handled = true;
    }

    private void ToggleSearchTickets(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TicketsSendingViewModel vm) {
            vm.IsSearchExpanded = !vm.IsSearchExpanded;
            Console.WriteLine($"[DEBUG] TicketsSendingView: Toggled SearchTickets to {vm.IsSearchExpanded}");
        }
    }

    private void ToggleTicketResults(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TicketsSendingViewModel vm) {
            vm.IsResultsExpanded = !vm.IsResultsExpanded;
            Console.WriteLine($"[DEBUG] TicketsSendingView: Toggled TicketResults to {vm.IsResultsExpanded}");
        }
    }

    private void ToggleTicketLines(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TicketsSendingViewModel vm) {
            vm.IsLinesExpanded = !vm.IsLinesExpanded;
            Console.WriteLine($"[DEBUG] TicketsSendingView: Toggled TicketLines to {vm.IsLinesExpanded}");
        }
    }

    private void ToggleCreateTicket(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TicketsSendingViewModel vm) {
            vm.IsCreateExpanded = !vm.IsCreateExpanded;
            Console.WriteLine($"[DEBUG] TicketsSendingView: Toggled CreateTicket to {vm.IsCreateExpanded}");
        }
    }

    private void ToggleScaleMeasurement(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TicketsSendingViewModel vm) {
            vm.IsScaleExpanded = !vm.IsScaleExpanded;
            Console.WriteLine($"[DEBUG] TicketsSendingView: Toggled ScaleMeasurement to {vm.IsScaleExpanded}");
        }
    }

    private void ToggleAddProductLines(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is TicketsSendingViewModel vm) {
            vm.IsAddLinesExpanded = !vm.IsAddLinesExpanded;
            Console.WriteLine($"[DEBUG] TicketsSendingView: Toggled AddProductLines to {vm.IsAddLinesExpanded}");
        }
    }

}
