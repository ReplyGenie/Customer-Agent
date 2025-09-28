using System.Text.Json;

namespace CustomerAgent.ConsoleApp.Configuration;

public static class ConfigurationLoader
{
    public static AppSettings Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"配置文件不存在: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
        if (settings is null)
        {
            throw new InvalidOperationException("无法解析配置文件");
        }

        return settings;
    }
}
