using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using MetalLink.Desktop.Services;

namespace MetalLink.Desktop.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(ThemeService themeService)
    {
        _themeService = themeService;
        _themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(IsDarkTheme));
    }

    public bool IsDarkTheme
    {
        get => _themeService.CurrentTheme == AppTheme.Dark;
        set
        {
            if (value == IsDarkTheme) return;
            _ = SetDarkAsync(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLightTheme));
        }
    }

    public bool IsLightTheme
    {
        get => _themeService.CurrentTheme == AppTheme.Light;
        set
        {
            if (value == IsLightTheme) return;
            _ = SetDarkAsync(!value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDarkTheme));
        }
    }

    private async Task SetDarkAsync(bool isDark)
    {
        try
        {
            await _themeService.SetThemeAsync(isDark ? AppTheme.Dark : AppTheme.Light);
            OnPropertyChanged(nameof(IsDarkTheme));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] Failed to update theme: " + ex);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
