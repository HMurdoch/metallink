using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetalLink.Api.Extensions;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Shared.Settings;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/operator-settings")]
[Authorize]
public sealed class OperatorSettingsController : ControllerBase
{
    private readonly MetalLinkDbContext _db;

    public OperatorSettingsController(MetalLinkDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OperatorSettingDto>>> GetAll(CancellationToken ct)
    {
        var operatorId = (int)User.GetOperatorId();
        var settings = await _db.OperatorSettings
            .AsNoTracking()
            .Include(x => x.Setting)
            .Include(x => x.SettingOption)
            .Where(x => x.OperatorId == operatorId && x.IsActive)
            .Select(x => new OperatorSettingDto
            {
                OperatorId = x.OperatorId,
                SettingId = x.SettingId,
                SettingName = x.Setting.SettingName,
                SettingOptionId = x.SettingOptionId,
                SettingOptionValue = x.SettingOption.SettingOptionValue,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return Ok(settings);
    }

    [HttpGet("theme")]
    [ProducesResponseType(typeof(ThemeSettingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTheme(CancellationToken ct)
    {
        var operatorId = (int)User.GetOperatorId();

        // If multiple actives somehow exist, take the most recently updated/created.
        var theme = await _db.OperatorSettings
            .AsNoTracking()
            .Where(x => x.OperatorId == operatorId
                        && x.Setting.SettingName == "theme")
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.TimeUpdated)
            .ThenByDescending(x => x.TimeCreated)
            .Select(x => x.SettingOption.SettingOptionValue)
            .FirstOrDefaultAsync(ct);

        return Ok(new ThemeSettingDto { Theme = string.IsNullOrWhiteSpace(theme) ? "dark" : theme });
    }

    [HttpPut("theme")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeSettingDto dto, CancellationToken ct)
    {
  

        if (dto == null || string.IsNullOrWhiteSpace(dto.Theme))
            return BadRequest("Theme is required.");

        var themeValue = dto.Theme.Trim().ToLowerInvariant();
        if (themeValue is not ("light" or "dark"))
            return BadRequest("Theme must be 'light' or 'dark'.");

        var operatorId = (int)User.GetOperatorId();

        var themeSetting = await _db.Settings
            .FirstOrDefaultAsync(s => s.SettingName == "theme", ct);

        if (themeSetting == null)
            return BadRequest("Theme setting not found in DB.");

        var themeOption = await _db.SettingOptions
            .FirstOrDefaultAsync(o => o.SettingId == themeSetting.SettingId && o.SettingOptionValue == themeValue, ct);

        if (themeOption == null)
            return BadRequest($"Theme option '{themeValue}' not found in DB.");

        // Transaction ensures we never violate partial unique index (only one active per operator+setting).
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Soft-deactivate any currently active record(s) (defensive)
        var actives = await _db.OperatorSettings
            .Where(x => x.OperatorId == operatorId
                        && x.SettingId == themeSetting.SettingId
                        && x.IsActive)
            .ToListAsync(ct);

        foreach (var active in actives)
        {
            // Domain entity has private setter, so we need EF property setter.
            _db.Entry(active).Property("IsActive").CurrentValue = false;
        }

        // Insert new active row (history is preserved)
        var newRow = new MetalLink.Domain.Entities.OperatorSetting(
            operatorId: operatorId,
            settingId: themeSetting.SettingId,
            settingOptionId: themeOption.SettingOptionId,
            createdByOperatorId: operatorId);

        await _db.OperatorSettings.AddAsync(newRow, ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return NoContent();
    }

    [HttpGet("crystaline")]
    [ProducesResponseType(typeof(CrystalineSettingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrystaline(CancellationToken ct)
    {
        var operatorId = (int)User.GetOperatorId();

        var raw = await _db.OperatorSettings
            .AsNoTracking()
            .Where(x => x.OperatorId == operatorId
                        && (x.Setting.SettingName.ToLower() == "crystaline" || x.Setting.SettingName.ToLower() == "crystalline"))
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.TimeUpdated)
            .ThenByDescending(x => x.TimeCreated)
            .Select(x => x.SettingOption.SettingOptionValue)
            .FirstOrDefaultAsync(ct);

        var enabled = string.IsNullOrWhiteSpace(raw) ? true : raw.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        return Ok(new CrystalineSettingDto { Crystaline = enabled });
    }

    [HttpPut("crystaline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCrystaline([FromBody] UpdateCrystalineSettingDto dto, CancellationToken ct)
    {
        var operatorId = (int)User.GetOperatorId();

        // Be tolerant to DB naming differences (crystaline vs crystalline) and allow auto-provisioning.
        var setting = await _db.Settings
            .FirstOrDefaultAsync(s => s.IsActive && (s.SettingName.ToLower() == "crystaline" || s.SettingName.ToLower() == "crystalline"), ct);

        if (setting == null)
        {
            // Auto-create setting if missing
            setting = new MetalLink.Domain.Entities.Setting("crystaline", "Use crystal/transparent panels instead of solid panels", operatorId);
            await _db.Settings.AddAsync(setting, ct);
            await _db.SaveChangesAsync(ct);

            // Create options
            await _db.SettingOptions.AddAsync(new MetalLink.Domain.Entities.SettingOption(setting.SettingId, "true", operatorId), ct);
            await _db.SettingOptions.AddAsync(new MetalLink.Domain.Entities.SettingOption(setting.SettingId, "false", operatorId), ct);
            await _db.SaveChangesAsync(ct);
        }

        var value = dto.Crystaline ? "true" : "false";
        var option = await _db.SettingOptions
            .FirstOrDefaultAsync(o => o.IsActive && o.SettingId == setting.SettingId && o.SettingOptionValue.ToLower() == value, ct);

        if (option == null)
        {
            // Auto-create missing option if needed
            option = new MetalLink.Domain.Entities.SettingOption(setting.SettingId, value, operatorId);
            await _db.SettingOptions.AddAsync(option, ct);
            await _db.SaveChangesAsync(ct);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var actives = await _db.OperatorSettings
            .Where(x => x.OperatorId == operatorId
                        && x.SettingId == setting.SettingId
                        && x.IsActive)
            .ToListAsync(ct);

        foreach (var active in actives)
        {
            _db.Entry(active).Property("IsActive").CurrentValue = false;
        }

        var newRow = new MetalLink.Domain.Entities.OperatorSetting(
            operatorId: operatorId,
            settingId: setting.SettingId,
            settingOptionId: option.SettingOptionId,
            createdByOperatorId: operatorId);

        await _db.OperatorSettings.AddAsync(newRow, ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return NoContent();
    }

    [HttpGet("enforce-buyer-company")]
    [ProducesResponseType(typeof(EnforceBuyerCompanySettingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnforceBuyerCompany(CancellationToken ct)
    {
        var operatorId = (int)User.GetOperatorId();

        var raw = await _db.OperatorSettings
            .AsNoTracking()
            .Where(x => x.OperatorId == operatorId
                        && x.Setting.SettingName.ToLower() == "enforcebuyercompany")
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.TimeUpdated)
            .ThenByDescending(x => x.TimeCreated)
            .Select(x => x.SettingOption.SettingOptionValue)
            .FirstOrDefaultAsync(ct);

        // Default to true if not set
        var enabled = string.IsNullOrWhiteSpace(raw) ? true : raw.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        return Ok(new EnforceBuyerCompanySettingDto { EnforceBuyerCompany = enabled });
    }

    [HttpPut("enforce-buyer-company")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEnforceBuyerCompany([FromBody] UpdateEnforceBuyerCompanySettingDto dto, CancellationToken ct)
    {
        var operatorId = (int)User.GetOperatorId();

        var setting = await _db.Settings
            .FirstOrDefaultAsync(s => s.IsActive && s.SettingName.ToLower() == "enforcebuyercompany", ct);

        if (setting == null)
        {
            setting = new MetalLink.Domain.Entities.Setting("enforcebuyercompany", "Enforce buyer as company by default", operatorId);
            await _db.Settings.AddAsync(setting, ct);
            await _db.SaveChangesAsync(ct);

            await _db.SettingOptions.AddAsync(new MetalLink.Domain.Entities.SettingOption(setting.SettingId, "true", operatorId), ct);
            await _db.SettingOptions.AddAsync(new MetalLink.Domain.Entities.SettingOption(setting.SettingId, "false", operatorId), ct);
            await _db.SaveChangesAsync(ct);
        }

        var value = dto.EnforceBuyerCompany ? "true" : "false";
        var option = await _db.SettingOptions
            .FirstOrDefaultAsync(o => o.IsActive && o.SettingId == setting.SettingId && o.SettingOptionValue.ToLower() == value, ct);

        if (option == null)
        {
            option = new MetalLink.Domain.Entities.SettingOption(setting.SettingId, value, operatorId);
            await _db.SettingOptions.AddAsync(option, ct);
            await _db.SaveChangesAsync(ct);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var actives = await _db.OperatorSettings
            .Where(x => x.OperatorId == operatorId
                        && x.SettingId == setting.SettingId
                        && x.IsActive)
            .ToListAsync(ct);

        foreach (var active in actives)
        {
            _db.Entry(active).Property("IsActive").CurrentValue = false;
        }

        var newRow = new MetalLink.Domain.Entities.OperatorSetting(
            operatorId: operatorId,
            settingId: setting.SettingId,
            settingOptionId: option.SettingOptionId,
            createdByOperatorId: operatorId);

        await _db.OperatorSettings.AddAsync(newRow, ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return NoContent();
    }
}
