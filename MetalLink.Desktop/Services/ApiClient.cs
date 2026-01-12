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

        Console.WriteLine($"GET: {_httpClient.BaseAddress}{relativeUrl}");

        var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");
        }

        // Special handling for byte arrays (binary data like images)
        if (typeof(TResponse) == typeof(byte[]))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return (TResponse)(object)bytes;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }


    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();

        var response = await _httpClient.PostAsJsonAsync(
            relativeUrl,
            requestBody,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("========== API POST ERROR ==========");
            Console.WriteLine($"URL: {relativeUrl}");
            Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine("REQUEST BODY:");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("RESPONSE BODY:");
            Console.WriteLine(responseBody);
            Console.WriteLine("====================================");

            throw new HttpRequestException(
                $"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(
            cancellationToken: cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(
        string uri,
        T body,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();
        Console.WriteLine($"PUT: {_httpClient.BaseAddress}{uri}");
        
        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = JsonContent.Create(body)
        };

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        ApplyAuthHeader();

        Console.WriteLine($"DELETE: {_httpClient.BaseAddress}{relativeUrl}");

        var response = await _httpClient.DeleteAsync(relativeUrl, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"API {(int)response.StatusCode} {response.ReasonPhrase}. Body: {raw}");
    }


    public string ToQueryString(object? values)
    {
        if (values == null) return string.Empty;

        var props =
            from p in values.GetType().GetProperties()
            let value = p.GetValue(values, null)
            where value != null
            select $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(value.ToString()!)}";

        var qs = string.Join("&", props);
        return string.IsNullOrEmpty(qs) ? string.Empty : "?" + qs;
    }
}
