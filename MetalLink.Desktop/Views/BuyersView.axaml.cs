using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class BuyersView : UserControl
{
    public BuyersView()
    {
        InitializeComponent();
        // NOTE: Selection is already bound via:
        // SelectedItem="{Binding FoundBuyer, Mode=TwoWay}"
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

    private void ToggleBuyerDetails(object? sender, PointerPressedEventArgs e)
    {
        var arrow = this.FindControl<TextBlock>("BuyerDetailsArrow");
        var content = this.FindControl<StackPanel>("BuyerDetailsContent");
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
