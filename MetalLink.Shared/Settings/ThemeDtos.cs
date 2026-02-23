namespace MetalLink.Shared.Settings;

public sealed class ThemeSettingDto
{
    /// <summary>
    /// Theme value (e.g. "light" or "dark")
    /// </summary>
    public string Theme { get; set; } = "dark";
}

public sealed class UpdateThemeSettingDto
{
    /// <summary>
    /// Theme value (e.g. "light" or "dark")
    /// </summary>
    public string Theme { get; set; } = "dark";
}
