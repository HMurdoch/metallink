using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Configuration;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.Views;

namespace MetalLink.Desktop;

public partial class App : Application
{
    // Simple manual DI container
    public AuthState AuthState { get; } = new();
    public ApiClient ApiClient { get; private set; } = null!;
    public AuthService AuthService { get; private set; } = null!;
    public CustomerService CustomerService { get; private set; } = null!;
    public TicketService TicketService { get; private set; } = null!;
    public IScaleService ScaleService { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize services
        ApiClient = new ApiClient(AuthState);
        AuthService = new AuthService(AuthState);
        CustomerService = new CustomerService(ApiClient, AuthState);
        TicketService = new TicketService(ApiClient, AuthState);

        // Choose scale implementation
        if (ScaleConfig.UseMockScales)
        {
            ScaleService = new MockScaleService();
        }
        else
        {
            ScaleService = new SerialPortScaleService();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Start at Login window
            var loginWindow = new LoginWindow
            {
                DataContext = new LoginViewModel(this)
            };

            desktop.MainWindow = loginWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
