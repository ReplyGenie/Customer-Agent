using System.Text.Json;
using CustomerAgent.ConsoleApp.Configuration;
using CustomerAgent.ConsoleApp.Domain.Entities;
using CustomerAgent.ConsoleApp.Infrastructure.Http;

namespace CustomerAgent.ConsoleApp.Services;

public class PddAccountService
{
    private readonly AppSettings _settings;

    public PddAccountService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<(string userId, string username, string mallId)> GetUserInfoAsync(Account account, CancellationToken cancellationToken)
    {
        using var client = new PddRequestClient(_settings);
        client.UpdateCookies(account.Cookies);
        using var document = await client.PostRawAsync("https://mms.pinduoduo.com/janus/api/new/userinfo", string.Empty, cancellationToken)
            ?? throw new InvalidOperationException("获取用户信息失败: 空响应");

        var root = document.RootElement;
        if (!root.TryGetProperty("success", out var successElement) || successElement.ValueKind != JsonValueKind.True)
        {
            var errorMsg = root.TryGetProperty("errorMsg", out var error) ? error.GetString() : "未知错误";
            throw new InvalidOperationException($"获取用户信息失败: {errorMsg}");
        }

        var result = root.GetProperty("result");
        var userId = result.GetProperty("id").GetString() ?? throw new InvalidOperationException("响应缺少id");
        var username = result.GetProperty("username").GetString() ?? string.Empty;
        var mallId = result.GetProperty("mall_id").GetString() ?? string.Empty;
        return (userId, username, mallId);
    }

    public async Task<(string shopId, string shopName, string? mallLogo)> GetShopInfoAsync(Account account, CancellationToken cancellationToken)
    {
        using var client = new PddRequestClient(_settings);
        client.UpdateCookies(account.Cookies);
        using var document = await client.PostJsonAsync("https://mms.pinduoduo.com/earth/api/merchant/queryMerchantInfoByMallId", new { }, cancellationToken)
            ?? throw new InvalidOperationException("获取店铺信息失败: 空响应");

        var root = document.RootElement;
        if (!root.TryGetProperty("success", out var successElement) || successElement.ValueKind != JsonValueKind.True)
        {
            var errorMsg = root.TryGetProperty("errorMsg", out var error) ? error.GetString() : "未知错误";
            throw new InvalidOperationException($"获取店铺信息失败: {errorMsg}");
        }

        var result = root.GetProperty("result");
        var shopId = result.GetProperty("mallId").GetString() ?? throw new InvalidOperationException("响应缺少mallId");
        var shopName = result.GetProperty("mallName").GetString() ?? string.Empty;
        var mallLogo = result.TryGetProperty("mallLogo", out var logoElement) ? logoElement.GetString() : null;
        return (shopId, shopName, mallLogo);
    }

    public async Task<string> GetTokenAsync(Account account, CancellationToken cancellationToken)
    {
        using var client = new PddRequestClient(_settings);
        client.UpdateCookies(account.Cookies);
        using var document = await client.PostJsonAsync("https://mms.pinduoduo.com/chats/getToken", new { version = "3" }, cancellationToken)
            ?? throw new InvalidOperationException("获取token失败: 空响应");

        var root = document.RootElement;
        if (root.TryGetProperty("token", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String)
        {
            return tokenElement.GetString()!;
        }

        if (root.TryGetProperty("result", out var resultElement) && resultElement.TryGetProperty("token", out var tokenValue))
        {
            return tokenValue.GetString() ?? throw new InvalidOperationException("响应中token为空");
        }

        throw new InvalidOperationException("响应中未包含token字段");
    }
}
