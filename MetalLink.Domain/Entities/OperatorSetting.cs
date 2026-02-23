using System;

namespace MetalLink.Domain.Entities;

public class OperatorSetting
{
    public int OperatorSettingId { get; private set; }

    public int OperatorId { get; private set; }
    public Operator Operator { get; set; } = null!;

    public int SettingId { get; private set; }
    public Setting Setting { get; set; } = null!;

    public int SettingOptionId { get; private set; }
    public SettingOption SettingOption { get; set; } = null!;

    public bool IsActive { get; private set; } = true;

    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    public DateTimeOffset TimeCreated { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset TimeUpdated { get; private set; } = DateTimeOffset.UtcNow;

    private OperatorSetting() { }

    public OperatorSetting(int operatorId, int settingId, int settingOptionId, int createdByOperatorId)
    {
        OperatorId = operatorId;
        SettingId = settingId;
        SettingOptionId = settingOptionId;
        CreatedByOperatorId = createdByOperatorId;
    }
}
