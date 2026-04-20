using System;
using System.Collections.Generic;

namespace MetalLink.Domain.Entities;

public class Setting
{
    public int SettingId { get; private set; }

    public string SettingName { get; private set; } = string.Empty;
    public string? SettingDescription { get; private set; }

    public bool IsActive { get; private set; } = true;

    public int CreatedByOperatorId { get; private set; }
    public Operator CreatedByOperator { get; set; } = null!;

    public DateTimeOffset TimeCreated { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset TimeUpdated { get; private set; } = DateTimeOffset.UtcNow;

    public ICollection<SettingOption> Options { get; set; } = new List<SettingOption>();

    private Setting() { }

    public Setting(string settingName, string? settingDescription, int createdByOperatorId)
    {
        SettingName = settingName;
        SettingDescription = settingDescription;
        CreatedByOperatorId = createdByOperatorId;
    }
}
