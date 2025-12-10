using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Shared.Provinces;

namespace MetalLink.Desktop.Services;

public sealed class ProvinceService
{
    private readonly ApiClient _apiClient;

    public ProvinceService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<ProvinceDto>> GetAllAsync()
    {
        var result = await _apiClient.GetAsync<ProvinceDto[]>("api/provinces");
        return result ?? Array.Empty<ProvinceDto>();
    }
}
