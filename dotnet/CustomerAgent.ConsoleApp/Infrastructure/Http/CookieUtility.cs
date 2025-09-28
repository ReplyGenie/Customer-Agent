using System.Linq;
using System.Text.Json;

namespace CustomerAgent.ConsoleApp.Infrastructure.Http;

public static class CookieUtility
{
    public static Dictionary<string, string> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        input = input.Trim();
        if (input.StartsWith("{") && input.EndsWith("}"))
        {
            try
            {
                var document = JsonDocument.Parse(input);
                return document.RootElement
                    .EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                // fall back to semi-colon parsing
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var segments = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split('=', 2);
            if (parts.Length == 2)
            {
                result[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return result;
    }

    public static string BuildCookieHeader(IReadOnlyDictionary<string, string> cookies)
    {
        return string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}
