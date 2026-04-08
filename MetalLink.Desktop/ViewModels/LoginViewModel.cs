using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
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

                // 1) Show Intro Window
                intro.Topmost = true; // Ensure it's on top of everything
                intro.Show();

                // 2) Play Intro Sequence in parallel with preparing MainWindow
                var introSetting = _app.AuthState.OperatorSettings.FirstOrDefault(s => s.SettingName.Equals("playintrovideo", StringComparison.OrdinalIgnoreCase));
                bool playVideo = introSetting == null || introSetting.SettingOptionValue.Equals("true", StringComparison.OrdinalIgnoreCase);

                // Start playing intro
                Console.WriteLine("[DEBUG] Starting intro playback task...");
                var introTask = intro.PlayAsync(playVideo);

                // Prepare MainWindow in the background (don't call Show yet)
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(_app)
                };
                var vm = (MainWindowViewModel)mainWindow.DataContext;
                _ = vm.InitializeLookupsAsync();

                // Initial size/position for MainWindow
                mainWindow.Width = 1920;
                mainWindow.Height = 950;
                
                if (loginWindow != null)
                {
                    var screen = loginWindow.Screens.ScreenFromWindow(loginWindow) ?? loginWindow.Screens.Primary;
                    if (screen != null)
                    {
                        var bounds = screen.WorkingArea;
                        mainWindow.Position = new PixelPoint(
                            bounds.X + (bounds.Width - 1920) / 2,
                            bounds.Y + (bounds.Height - 950) / 2);
                    }
                }

                // Close login window
                loginWindow?.Close();

                // Await intro with a global timeout as a failsafe
                var timeoutTask = Task.Delay(15000); // 15s max for intro failsafe
                var completedTask = await Task.WhenAny(introTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("[WARN] Intro sequence timed out at 15s. Forcing transition to MainWindow.");
                }
                else
                {
                    Console.WriteLine("[DEBUG] Intro sequence completed or was skipped.");
                }

                // 3) Switch to Main Window
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Console.WriteLine("[DEBUG] Switching to MainWindow...");
                    
                    // Crucial: Set the new MainWindow BEFORE showing it
                    desktop.MainWindow = mainWindow;
                    
                    Console.WriteLine("[DEBUG] Calling mainWindow.Show()...");
                    mainWindow.Show();
                    
                    Console.WriteLine("[DEBUG] Setting WindowState to Normal...");
                    mainWindow.WindowState = WindowState.Normal;
                    
                    Console.WriteLine("[DEBUG] Calling mainWindow.Activate()...");
                    mainWindow.Activate();
                    
                    // 4) Cleanup Intro
                    Console.WriteLine("[DEBUG] Closing IntroWindow.");
                    intro.Close();
                });
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
