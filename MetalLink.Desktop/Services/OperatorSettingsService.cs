using System.Threading.Tasks;
using MetalLink.Shared.Settings;

namespace MetalLink.Desktop.Services;

public sealed class OperatorSettingsService
{
    private readonly ApiClient _apiClient;

    public OperatorSettingsService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task UpdateSettingAsync(int settingId, int optionId)
    {
        // endpoint matches the logic used in AppearanceService but generic for any setting
        await _apiClient.PutAsJsonAsync($"api/operator-settings/{settingId}", new { SettingOptionId = optionId });
    }
}
