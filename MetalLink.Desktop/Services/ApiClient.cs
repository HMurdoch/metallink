using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Configuration;

namespace MetalLink.Desktop.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthState _authState;

    public ApiClient(AuthState authState)
    {
        _authState = authState;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl)
        };
    }

    private void ApplyAuthHeader()
    {
        if (_authState.IsAuthenticated)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authState.Token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        var response = await _httpClient.PostAsJsonAsync(relativeUrl, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    public string ToQueryString(object? values)
    {
        if (values == null) return string.Empty;

        var props = from p in values.GetType().GetProperties()
                    let value = p.GetValue(values, null)
                    where value != null
                    select $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(value.ToString()!)}";

        var qs = string.Join("&", props);
        return string.IsNullOrEmpty(qs) ? string.Empty : "?" + qs;
    }

//     public string ToQueryString(object? obj)
//     {
//         if (obj == null)
//             return string.Empty;

//         var properties = obj
//             .GetType()
//             .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
//             .Where(p => p.CanRead)
//             .Select(p => new
//             {
//                 Name = p.Name,
//                 Value = p.GetValue(obj)
//             })
//             .Where(x => x.Value != null && !string.IsNullOrWhiteSpace(x.Value.ToString()))
//             .ToList();

//         if (!properties.Any())
//             return string.Empty;

//         var query = string.Join("&", properties.Select(x =>
//             $"{Uri.EscapeDataString(x.Name)}={Uri.EscapeDataString(x.Value!.ToString()!)}"));

//         return "?" + query;
//     }
}
