using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MetalLink.Desktop.Auth;
using MetalLink.Desktop.Configuration;

namespace MetalLink.Desktop.Services;

public sealed class TicketReportService
{
    private readonly AuthState _authState;
    private readonly HttpClient _httpClient;

    public TicketReportService(AuthState authState)
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

    public async Task<string> DownloadTicketReportAsync(
        long ticketId,
        CancellationToken cancellationToken = default)
    {
        ApplyAuthHeader();

        var url = $"api/tickets/{ticketId}/report";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"API returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var reportsFolder = Path.Combine(documents, "MetalLinkReports");

        if (!Directory.Exists(reportsFolder))
            Directory.CreateDirectory(reportsFolder);

        var fileName = $"ticket-{ticketId}-{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var fullPath = Path.Combine(reportsFolder, fileName);

        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        // Try open with default PDF viewer
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // Ignore open errors – we still return the path
        }

        return fullPath;
    }
}
