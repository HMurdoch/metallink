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
    public CompanyAndSiteService CompanyAndSiteService { get; private set; } = null!;
    public TicketService TicketService { get; private set; } = null!;
    public IScaleService ScaleService { get; private set; } = null!;
    public DocumentService DocumentService { get; private set; } = null!;
    public ICameraService CameraService { get; private set; } = null!;
    public TicketReportService TicketReportService { get; private set; } = null!;
    public ISignaturePadService SignaturePadService { get; private set; } = null!;

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
        CompanyAndSiteService = new CompanyAndSiteService(ApiClient, AuthState);
        TicketService = new TicketService(ApiClient, AuthState);
        ScaleService = new MockScaleService();
        DocumentService = new DocumentService(ApiClient, AuthState);
        CameraService = new MockCameraService();
        TicketReportService = new TicketReportService(AuthState);
        SignaturePadService = new MockSignaturePadService(); // swap to real device later

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // If you later re-enable login, switch back to LoginWindow here.
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
