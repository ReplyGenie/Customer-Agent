using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using CustomerAgent.ConsoleApp.Domain.Entities;
using CustomerAgent.ConsoleApp.Domain.Messaging;
using CustomerAgent.ConsoleApp.Services;

namespace CustomerAgent.ConsoleApp.Infrastructure.WebSockets;

public class PddWebSocketClient
{
    private const string BaseUrl = "wss://m-ws.pinduoduo.com/";
    private readonly Channel<PddUserMessage> _messageChannel;

    public PddWebSocketClient(Channel<PddUserMessage> messageChannel)
    {
        _messageChannel = messageChannel;
    }

    public async Task RunAsync(Account account, string accessToken, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["access_token"] = accessToken,
            ["role"] = "mall_cs",
            ["client"] = "web",
            ["version"] = "202506091557"
        };

        var query = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var uri = new Uri($"{BaseUrl}?{query}");

        using var webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

        await webSocket.ConnectAsync(uri, cancellationToken);

        var buffer = new byte[32 * 1024];
        var builder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            builder.Clear();
            WebSocketReceiveResult? result;
            do
            {
                result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", cancellationToken);
                    return;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                builder.Append(chunk);
            } while (!result.EndOfMessage);

            var payload = builder.ToString();
            if (!PddMessageParser.TryParse(account.ShopId ?? string.Empty, payload, out var message, out var warning))
            {
                if (warning is not null)
                {
                    Console.WriteLine($"[WS] {warning}");
                }
                continue;
            }

            if (message is not null)
            {
                await _messageChannel.Writer.WriteAsync(message, cancellationToken);
            }
        }
    }
}
