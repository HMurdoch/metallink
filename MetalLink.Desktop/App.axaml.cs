using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Hardware;
using MetalLink.Desktop.Services;
using MetalLink.Desktop.ViewModels;
using MetalLink.Desktop.Views;

namespace MetalLink.Desktop;

public partial class App : Application
{
    public AuthState AuthState { get; private set; } = new();
    public ApiClient ApiClient { get; private set; } = null!;
    public AuthService AuthService { get; private set; } = null!;
    public CustomerService CustomerService { get; private set; } = null!;
    public TicketService TicketService { get; private set; } = null!;
    public IScaleService ScaleService { get; private set; } = null!;
    public DocumentService DocumentService { get; private set; } = null!;
    public ICameraService CameraService { get; private set; } = null!;

    public override void Initialize()
    {
        // IMPORTANT: use AvaloniaXamlLoader here, NOT InitializeComponent()
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize services
        ApiClient = new ApiClient(AuthState);
        AuthService = new AuthService(AuthState);
        CustomerService = new CustomerService(ApiClient, AuthState);
        TicketService = new TicketService(ApiClient, AuthState);
        ScaleService = new MockScaleService();
        DocumentService = new DocumentService(ApiClient, AuthState);
        CameraService = new MockCameraService(); // later swap for real impl

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // var loginVm = new LoginViewModel(this);
            // desktop.MainWindow = new LoginWindow
            // {
            //     DataContext = loginVm
            // };
            desktop.MainWindow = new LoginWindow
            {
                DataContext = new LoginViewModel(this)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
