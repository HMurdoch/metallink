using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(viewModel.FoundCustomer) && viewModel.FoundCustomer != null)
                    {
                        // Trigger edit when a customer is selected from results
                        if (viewModel.EditCustomerCommand.CanExecute(viewModel.FoundCustomer))
                        {
                            viewModel.EditCustomerCommand.Execute(viewModel.FoundCustomer);
                        }
                    }
                };
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
