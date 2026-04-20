using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MetalLink.Desktop.ViewModels;

namespace MetalLink.Desktop.Views;

public partial class LoginWindow : Window
{
    private TextBox? _passwordBox;
    private Button? _toggleButton;
    private Button? _loginButton;
    private TextBlock? _eyeIcon;
    private LoginViewModel? _viewModel;
    private bool _isPasswordVisible;
    private string _actualPassword = string.Empty;

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        _passwordBox = this.FindControl<TextBox>("PasswordBox");
        _toggleButton = this.FindControl<Button>("TogglePasswordButton");
        _loginButton = this.FindControl<Button>("LoginButton");
        _eyeIcon = this.FindControl<TextBlock>("EyeIcon");

        if (_loginButton != null)
        {
            _loginButton.Focus();
        }

        if (_toggleButton != null)
        {
            _toggleButton.Click += OnTogglePasswordClick;
        }

        if (_passwordBox != null)
        {
            _passwordBox.TextChanged += OnPasswordBoxTextChanged;
        }

        // When data context is set, initialize with the hardcoded password
        DataContextChanged += (s, e) =>
        {
            _viewModel = DataContext as LoginViewModel;
            if (_viewModel != null)
            {
                _actualPassword = _viewModel.Password;
                _isPasswordVisible = false;
                UpdatePasswordDisplay();
            }
        };
    }

    private void OnPasswordBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_passwordBox == null || _viewModel == null) return;

        string inputText = _passwordBox.Text ?? string.Empty;

        // If password is masked, extract the actual characters typed
        if (!_isPasswordVisible)
        {
            // Count non-bullet characters as new input
            int bulletCount = inputText.Count(c => c == '•');
            int newCharCount = inputText.Length - bulletCount;

            if (newCharCount > 0)
            {
                // Extract the new characters (non-bullets)
                var newChars = inputText.Where(c => c != '•').ToList();
                _actualPassword = new string(newChars.ToArray());
            }
            else if (inputText.Length < _actualPassword.Length)
            {
                // User deleted characters
                _actualPassword = _actualPassword.Substring(0, inputText.Length);
            }
        }
        else
        {
            // Password is visible, so we can directly use what's typed
            _actualPassword = inputText;
        }

        _viewModel.Password = _actualPassword;
        UpdatePasswordDisplay();
    }

    private void OnTogglePasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        UpdatePasswordDisplay();
    }

    private void UpdatePasswordDisplay()
    {
        if (_passwordBox == null || _eyeIcon == null) return;

        if (_isPasswordVisible)
        {
            _passwordBox.Text = _actualPassword;
            _eyeIcon.Text = "👁️"; // Open eye
        }
        else
        {
            _passwordBox.Text = new string('•', _actualPassword.Length);
            _eyeIcon.Text = "🚫"; // Closed eye (blocked)
        }
    }
}
