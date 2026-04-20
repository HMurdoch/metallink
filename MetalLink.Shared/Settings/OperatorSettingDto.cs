using System;

namespace MetalLink.Shared.Settings;

public class OperatorSettingDto
{
    public int OperatorId { get; set; }
    public int SettingId { get; set; }
    public string SettingName { get; set; } = string.Empty;
    public int SettingOptionId { get; set; }
    public string SettingOptionValue { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
