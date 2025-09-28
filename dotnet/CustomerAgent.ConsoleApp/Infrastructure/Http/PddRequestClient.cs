using System.Text;
using System.Text.Json;
using CustomerAgent.ConsoleApp.Configuration;

namespace CustomerAgent.ConsoleApp.Infrastructure.Http;

public class PddRequestClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _defaultHeaders;
    private IReadOnlyDictionary<string, string> _cookies;

    public PddRequestClient(AppSettings settings)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _defaultHeaders = new Dictionary<string, string>(settings.Pdd.DefaultHeaders, StringComparer.OrdinalIgnoreCase);
        _cookies = new Dictionary<string, string>();
    }

    public void UpdateCookies(IReadOnlyDictionary<string, string> cookies)
    {
        _cookies = new Dictionary<string, string>(cookies, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<JsonDocument?> PostJsonAsync(string url, object? payload, CancellationToken cancellationToken)
    {
        var content = payload is null
            ? new StringContent("{}", Encoding.UTF8, "application/json")
            : new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        return await SendAsync(url, content, cancellationToken);
    }

    public async Task<JsonDocument?> PostRawAsync(string url, string body, CancellationToken cancellationToken)
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        return await SendAsync(url, content, cancellationToken);
    }

    private async Task<JsonDocument?> SendAsync(string url, HttpContent content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        foreach (var header in _defaultHeaders)
        {
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (_cookies.Count > 0)
        {
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", CookieUtility.BuildCookieHeader(_cookies));
        }

        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var contentString = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"请求失败: {(int)response.StatusCode} {response.ReasonPhrase}\n{contentString}");
                }

                if (string.IsNullOrWhiteSpace(contentString))
                {
                    return null;
                }

                return JsonDocument.Parse(contentString);
            }
            catch (Exception) when (attempt < maxRetries)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2 + Random.Shared.Next(100, 400));
            }
        }

        return null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
