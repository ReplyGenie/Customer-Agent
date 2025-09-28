using System.Linq;
using CustomerAgent.ConsoleApp.Domain.Entities;

namespace CustomerAgent.ConsoleApp.Infrastructure.Persistence;

public class InMemoryDatabase
{
    private readonly Dictionary<Guid, Account> _accounts = new();
    private readonly Dictionary<string, Shop> _shops = new(StringComparer.OrdinalIgnoreCase);

    public Account UpsertAccount(Account account)
    {
        _accounts[account.Id] = account;
        return account;
    }

    public IReadOnlyCollection<Account> GetAccounts() => _accounts.Values.ToList();

    public Account? FindAccountByUserId(string userId)
    {
        return _accounts.Values.FirstOrDefault(a => string.Equals(a.UserId, userId, StringComparison.OrdinalIgnoreCase));
    }

    public void SaveShop(Shop shop)
    {
        _shops[shop.ShopId] = shop;
    }

    public Shop? GetShop(string shopId)
    {
        _shops.TryGetValue(shopId, out var shop);
        return shop;
    }
}
