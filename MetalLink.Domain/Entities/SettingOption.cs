using System;

namespace MetalLink.Domain.Entities;

public class SettingOption
{
    public int SettingOptionId { get; private set; }

    public int SettingId { get; private set; }
    public Setting Setting { get; set; } = null!;

    public string SettingOptionValue { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    public DateTimeOffset TimeCreated { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset TimeUpdated { get; private set; } = DateTimeOffset.UtcNow;

    private SettingOption() { }

    public SettingOption(int settingId, string settingOptionValue, int createdByOperatorId)
    {
        SettingId = settingId;
        SettingOptionValue = settingOptionValue;
        CreatedByOperatorId = createdByOperatorId;
    }
}
