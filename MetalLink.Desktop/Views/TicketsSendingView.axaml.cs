using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

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
}
