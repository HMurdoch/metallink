using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        // NOTE: Selection is already handled via DataGrid SelectedItem binding.
        // Do NOT execute commands from PropertyChanged here; it causes recursion.
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ToggleSearchCriteria(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("SearchCriteriaArrow");
        var content = this.FindControl<Grid>("SearchCriteriaContent");
        TogglePanel(arrow, content);
    }

    private void ToggleSearchResults(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("SearchResultsArrow");
        var content = this.FindControl<Grid>("SearchResultsContent");
        TogglePanel(arrow, content);
    }

    private void ToggleCustomerDetails(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("CustomerDetailsArrow");
        var content = this.FindControl<StackPanel>("CustomerDetailsContent");
        TogglePanel(arrow, content);
    }

    private void ToggleCreateEdit(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("CreateEditArrow");
        var content = this.FindControl<Grid>("CreateEditContent");
        TogglePanel(arrow, content);
    }

    private void TogglePanel(TextBlock? arrow, Control? content)
    {
        if (arrow == null || content == null) return;
        
        bool isCollapsed = arrow.Text == "▶";
        arrow.Text = isCollapsed ? "▼" : "▶";
        content.IsVisible = isCollapsed;
    }
}
