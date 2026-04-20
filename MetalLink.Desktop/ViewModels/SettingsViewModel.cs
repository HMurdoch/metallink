using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using MetalLink.Desktop.Services;

namespace MetalLink.Desktop.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private bool _isAppearanceExpanded = true;
    public bool IsAppearanceExpanded { get => _isAppearanceExpanded; set { _isAppearanceExpanded = value; OnPropertyChanged(); } }

    private readonly ThemeService _themeService;
    private readonly AppearanceService _appearanceService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(ThemeService themeService, AppearanceService appearanceService)
    {
        _themeService = themeService;
        _appearanceService = appearanceService;
        _themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(IsDarkTheme));
        _appearanceService.CrystalineChanged += (_, _) => OnPropertyChanged(nameof(IsCrystaline));
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

    public bool IsCrystaline
    {
        get => _appearanceService.IsCrystaline;
        set
        {
            if (value == IsCrystaline) return;
            _ = SetCrystalineAsync(value);
        }
    }

    private MainWindowViewModel? _mainVm;
    private MainWindowViewModel? MainVm
    {
        get
        {
            if (_mainVm != null) return _mainVm;
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow?.DataContext is MainWindowViewModel vm)
                {
                    _mainVm = vm;
                }
            }
            return _mainVm;
        }
    }

    public bool EnforceBuyerCompany
    {
        get => MainVm?.EnforceBuyerCompany ?? true;
        set
        {
            if (MainVm != null && value != MainVm.EnforceBuyerCompany)
            {
                _ = MainVm.SetEnforceBuyerCompanyAsync(value);
                OnPropertyChanged();
            }
        }
    }

    public bool PlayIntroVideo
    {
        get => MainVm?.PlayIntroVideo ?? true;
        set
        {
            if (MainVm != null && value != MainVm.PlayIntroVideo)
            {
                MainVm.PlayIntroVideo = value;
                OnPropertyChanged();
            }
        }
    }

    private async Task SetCrystalineAsync(bool enabled)
    {
        try
        {
            await _appearanceService.SetCrystalineAsync(enabled);
            OnPropertyChanged(nameof(IsCrystaline));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ERROR] Failed to update crystaline: " + ex);

            // Re-sync from server (and re-apply locally) to avoid the toggle lying.
            try
            {
                await _appearanceService.LoadFromServerAsync();
            }
            catch (Exception reloadEx)
            {
                Console.Error.WriteLine("[ERROR] Failed to reload crystaline: " + reloadEx);
            }

            OnPropertyChanged(nameof(IsCrystaline));
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
