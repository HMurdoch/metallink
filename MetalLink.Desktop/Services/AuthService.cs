using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Configuration;
using MetalLink.Shared.Auth;

namespace MetalLink.Desktop.Services;

public sealed class AuthService
{
    private readonly AuthState _authState;
    private readonly HttpClient _httpClient;

    public AuthService(AuthState authState)
    {
        _authState = authState;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl)
        };
    }

    public async Task<bool> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var request = new LoginRequestDto
        {
            Username = username,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: cancellationToken);
        if (result is null || string.IsNullOrWhiteSpace(result.Token))
        {
            return false;
        }

        _authState.SetAuth(
            token: result.Token,
            username: result.Username,
            displayName: result.DisplayName,
            role: result.Role,
            siteId: result.SiteId
        );
        _authState.OperatorSettings = result.OperatorSettings;

        return true;
    }

    public void Logout()
    {
        _authState.Clear();
    }
}
