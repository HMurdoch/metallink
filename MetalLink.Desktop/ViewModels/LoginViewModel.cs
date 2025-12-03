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
        set { _statusMessage = value; OnPropertyChanged(); }
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

            // Switch to MainWindow
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(_app)
                };

                // Grab the current window (login) via the active MainWindow
                var loginWindow = desktop.MainWindow;

                // Switch the lifetime's main window to the new one
                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                // Hide (not close) the login window so ShutdownMode isn't triggered
                loginWindow?.Hide();
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
