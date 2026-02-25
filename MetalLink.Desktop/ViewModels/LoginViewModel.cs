using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MetalLink.Desktop.Services;
using MetalLink.Desktop.Views;

namespace MetalLink.Desktop.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly App _app;
    private string _username = "admin";
    private string _password = "Admin123!";
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value ?? string.Empty;
            OnPropertyChanged();

            // ALSO log to console so you see it in dotnet run output:
            Console.WriteLine($"[STATUS] {_statusMessage}");
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel(App app)
    {
        _app = app;
        LoginCommand = new AsyncCommand(LoginAsync);
    }

    private async Task LoginAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Logging in...";

        try
        {
            var success = await _app.AuthService.LoginAsync(Username, Password);

            if (!success)
            {
                StatusMessage = "Invalid username or password.";
                return;
            }

            StatusMessage = "Login successful.";

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Load and apply per-operator theme before we show the main window.
                try
                {
                    await _app.ThemeService.LoadFromServerAsync();
                    await _app.AppearanceService.LoadFromServerAsync();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[WARN] Failed to load theme/appearance from server: " + ex);
                }

                var loginWindow = desktop.MainWindow; // should be the LoginWindow

                // Show intro video window first (normal sized, centered)
                var intro = new IntroWindow();

                // Tell intro which monitor bounds to center within
                if (loginWindow != null && loginWindow.Screens.Primary != null)
                {
                    var loginScreen = loginWindow.Screens.ScreenFromWindow(loginWindow) ?? loginWindow.Screens.Primary;
                    var bounds = loginScreen.WorkingArea;
                    intro.SetTargetBounds(bounds);

                    // Initial center (before intro sizes itself to the video)
                    // so it doesn't flash in the wrong place.
                    var initialW = 600;
                    var initialH = 900;
                    intro.Width = initialW;
                    intro.Height = initialH;
                    intro.Position = new Avalonia.PixelPoint(
                        bounds.X + (bounds.Width - initialW) / 2,
                        bounds.Y + (bounds.Height - initialH) / 2);
                }

                desktop.MainWindow = intro;
                intro.Show();

                // Close login window immediately after intro shows
                loginWindow?.Close();

                // Play intro sequence (6 seconds total)
                await intro.PlayAsync();

                // Now create and show the main window
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(_app)
                };

                var vm = (MainWindowViewModel)mainWindow.DataContext;
                _ = vm.InitializeLookupsAsync();

                // Set MainWindow initial size to 1920x950
                mainWindow.Width = 1920;
                mainWindow.Height = 950;

                // Position on the same monitor as intro/login
                if (intro.Screens.Primary != null)
                {
                    var screen = intro.Screens.ScreenFromWindow(intro) ?? intro.Screens.Primary;
                    var bounds = screen.WorkingArea;
                    var centerX = bounds.X + (bounds.Width - 1920) / 2;
                    var centerY = bounds.Y + (bounds.Height - 950) / 2;
                    mainWindow.Position = new Avalonia.PixelPoint(centerX, centerY);
                }

                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;

                // Close intro after main window is shown
                intro.Close();
            }
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Cannot reach server: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Simple async command helper
    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;

        public AsyncCommand(Func<Task> execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter) => await _execute();
    }
}
