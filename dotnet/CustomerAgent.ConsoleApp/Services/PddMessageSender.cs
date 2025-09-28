using System.Text.Json;
using CustomerAgent.ConsoleApp.Configuration;
using CustomerAgent.ConsoleApp.Domain.Entities;
using CustomerAgent.ConsoleApp.Infrastructure.Http;

namespace CustomerAgent.ConsoleApp.Services;

public class PddMessageSender
{
    private readonly AppSettings _settings;

    public PddMessageSender(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task SendTextAsync(Account account, string recipientUid, string content, CancellationToken cancellationToken)
    {
        using var client = new PddRequestClient(_settings);
        client.UpdateCookies(account.Cookies);

        var payload = new
        {
            data = new
            {
                cmd = "send_message",
                request_id = GenerateRequestId(),
                message = new
                {
                    to = new { role = "user", uid = recipientUid },
                    from = new { role = "mall_cs" },
                    content,
                    msg_id = (string?)null,
                    type = 0,
                    is_aut = 0,
                    manual_reply = 1
                }
            },
            client = "WEB"
        };

        using var document = await client.PostJsonAsync("https://mms.pinduoduo.com/plateau/chat/send_message", payload, cancellationToken)
            ?? throw new InvalidOperationException("发送消息失败: 空响应");

        var root = document.RootElement;
        if (!root.TryGetProperty("success", out var successEl) || successEl.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException($"发送消息失败: {root.ToString()}");
        }

        if (root.TryGetProperty("result", out var resultEl) && resultEl.TryGetProperty("error_code", out var codeEl) && codeEl.ValueKind == JsonValueKind.Number && codeEl.GetInt32() == 10002)
        {
            var error = resultEl.TryGetProperty("error", out var errorEl) ? errorEl.GetString() : "未知错误";
            throw new InvalidOperationException($"发送消息失败: {error}");
        }
    }

    private static string GenerateRequestId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
