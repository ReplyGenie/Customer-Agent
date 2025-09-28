namespace CustomerAgent.ConsoleApp.Domain.Entities;

public class Account
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Channel { get; init; } = "pinduoduo";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? MallLogo { get; set; }
    public Dictionary<string, string> Cookies { get; private set; } = new();

    public void UpdateCookies(Dictionary<string, string> cookies)
    {
        Cookies = new Dictionary<string, string>(cookies, StringComparer.OrdinalIgnoreCase);
    }
}
