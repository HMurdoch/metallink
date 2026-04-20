using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using MetalLink.Shared.Settings;

namespace MetalLink.Desktop.Services;

public sealed class AppearanceService
{
    private readonly ApiClient _apiClient;

    public bool IsCrystaline { get; private set; } = true;

    public event EventHandler<bool>? CrystalineChanged;

    /// <summary>
    /// Re-apply the current appearance (Crystaline vs Solid) against the currently active theme.
    /// Useful after theme changes because we store resolved brush instances in Application resources.
    /// </summary>
    public void ReapplyForCurrentTheme()
    {
        ApplyToApplication(IsCrystaline);
    }

    public AppearanceService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task LoadFromServerAsync(CancellationToken ct = default)
    {
        var dto = await _apiClient.GetAsync<CrystalineSettingDto>("api/operator-settings/crystaline", ct);
        await ApplyAsync(dto?.Crystaline ?? true, persistToServer: false, ct);
    }

    public Task SetCrystalineAsync(bool enabled, CancellationToken ct = default)
        => ApplyAsync(enabled, persistToServer: true, ct);

    private async Task ApplyAsync(bool enabled, bool persistToServer, CancellationToken ct)
    {
        var previous = IsCrystaline;

        // Apply immediately for responsive UX
        IsCrystaline = enabled;
        ApplyToApplication(enabled);
        CrystalineChanged?.Invoke(this, enabled);

        if (!persistToServer)
            return;

        try
        {
            var payload = new UpdateCrystalineSettingDto { Crystaline = enabled };
            var resp = await _apiClient.PutAsJsonAsync("api/operator-settings/crystaline", payload, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Failed to update crystaline. {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {raw}");
            }
        }
        catch
        {
            // Revert locally on failure
            IsCrystaline = previous;
            ApplyToApplication(previous);
            CrystalineChanged?.Invoke(this, previous);
            throw;
        }
    }

    private static void ApplyToApplication(bool enabled)
    {
        if (Application.Current is null)
            return;

        // Swap the effective panel background resource.
        // Views should bind to Brush.EffectivePanelBackground.
        if (Application.Current.Resources is not { } res)
            return;

        // Resolve actual brush instances (theme-scoped resources live in ThemeDictionaries)
        // IMPORTANT: we cache the resolved brush instances into Application resources.
        // So if the theme changes, we must re-run this method to re-resolve from the new dictionary.
        var theme = Application.Current?.RequestedThemeVariant ?? Application.Current?.ActualThemeVariant;
        var crystal = res.TryGetResource("Brush.MainBackground", theme, out var c) ? c : null;
        var solid = res.TryGetResource("Brush.SolidPanelBackground", theme, out var s) ? s : null;

        var chosen = enabled ? crystal : solid;
        if (chosen is null)
            return;

        // Swap the panel background used by section containers across the app
        res["Brush.PanelBackground"] = chosen;

        // Swap the "glass" panels used by Dashboard/Reports/StockLevels/etc.
        if (enabled)
        {
            if (res.TryGetResource("Brush.GlassPanelCrystal", theme, out var gp))
                res["Brush.GlassPanel"] = gp;
            if (res.TryGetResource("Brush.GlassBorderCrystal", theme, out var gb))
                res["Brush.GlassBorder"] = gb;
        }
        else
        {
            // Solid mode: use the solid panel background and normal border.
            if (solid is not null)
                res["Brush.GlassPanel"] = solid;
            if (res.TryGetResource("Brush.BorderSubtle", theme, out var bb))
                res["Brush.GlassBorder"] = bb;
        }
    }
}
