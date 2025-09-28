namespace CustomerAgent.ConsoleApp.Configuration;

public class AppSettings
{
    public PddSettings Pdd { get; set; } = new();
}

public class PddSettings
{
    public BusinessHoursSettings BusinessHours { get; set; } = new();
    public Dictionary<string, string> DefaultHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class BusinessHoursSettings
{
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "23:00";
}
