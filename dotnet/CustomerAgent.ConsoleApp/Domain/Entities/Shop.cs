namespace CustomerAgent.ConsoleApp.Domain.Entities;

public class Shop
{
    public string ShopId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
}
