using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using MetalLink.Shared.Settings;

namespace MetalLink.Desktop.Services;

public enum AppTheme
{
    Light,
    Dark
}

public sealed class ThemeService
{
    private readonly ApiClient _apiClient;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;

    public event EventHandler<AppTheme>? ThemeChanged;

    public ThemeService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task LoadFromServerAsync(CancellationToken ct = default)
    {
        // Will throw if not authenticated. Caller should handle.
        var dto = await _apiClient.GetAsync<ThemeSettingDto>("api/operator-settings/theme", ct);

        var theme = (dto?.Theme ?? "dark").Trim().ToLowerInvariant();
        await ApplyAsync(theme == "light" ? AppTheme.Light : AppTheme.Dark, persistToServer: false, ct);
    }

    public Task SetThemeAsync(AppTheme theme, CancellationToken ct = default)
        => ApplyAsync(theme, persistToServer: true, ct);

    private async Task ApplyAsync(AppTheme theme, bool persistToServer, CancellationToken ct)
    {
        if (CurrentTheme == theme && !persistToServer)
        {
            ApplyToApplication(theme);
            return;
        }

        if (persistToServer)
        {
            var payload = new UpdateThemeSettingDto
            {
                Theme = theme == AppTheme.Light ? "light" : "dark"
            };

            // ApiClient has PutAsJsonAsync but returns HttpResponseMessage; keep consistent.
            var resp = await _apiClient.PutAsJsonAsync("api/operator-settings/theme", payload, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Failed to update theme. {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {raw}");
            }
        }

        CurrentTheme = theme;
        ApplyToApplication(theme);
        ThemeChanged?.Invoke(this, theme);
    }

    private static void ApplyToApplication(AppTheme theme)
    {
        // Avalonia built-in theme switching.
        if (Application.Current is null) return;

        Application.Current.RequestedThemeVariant = theme == AppTheme.Light
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}
