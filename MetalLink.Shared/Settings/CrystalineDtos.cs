namespace MetalLink.Shared.Settings;

public sealed class CrystalineSettingDto
{
    /// <summary>
    /// True = crystal/transparent panels; False = solid panels.
    /// </summary>
    public bool Crystaline { get; set; }
}

public sealed class UpdateCrystalineSettingDto
{
    public bool Crystaline { get; set; }
}
