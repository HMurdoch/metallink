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
}
