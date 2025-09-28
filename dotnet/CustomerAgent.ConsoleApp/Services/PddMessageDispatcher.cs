using System.Threading.Channels;
using CustomerAgent.ConsoleApp.Domain.Entities;
using CustomerAgent.ConsoleApp.Domain.Messaging;

namespace CustomerAgent.ConsoleApp.Services;

public class PddMessageDispatcher
{
    private readonly Channel<PddUserMessage> _channel;
    private readonly PddMessageSender _sender;
    private readonly Account _account;

    public PddMessageDispatcher(Channel<PddUserMessage> channel, PddMessageSender sender, Account account)
    {
        _channel = channel;
        _sender = sender;
        _account = account;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (message.ContextType == ContextType.SystemStatus || message.ContextType == ContextType.MallSystemMessage)
            {
                Console.WriteLine($"[系统消息] {message.Text}");
                continue;
            }

            Console.WriteLine("----------------------------------------");
            Console.WriteLine($"时间: {FormatTimestamp(message.Timestamp)}");
            Console.WriteLine($"用户: {message.Nickname ?? message.UserUid}");
            Console.WriteLine($"类型: {message.ContextType}");
            Console.WriteLine($"内容: {message.Text}");

            if (message.ContextType != ContextType.Text && message.ContextType != ContextType.GoodsInquiry && message.ContextType != ContextType.OrderInfo)
            {
                Console.WriteLine("该消息类型不支持直接回复，按回车继续...");
                Console.ReadLine();
                continue;
            }

            Console.WriteLine("请输入回复内容(留空跳过，输入 /exit 退出):");
            var reply = Console.ReadLine();
            if (reply is null)
            {
                continue;
            }

            reply = reply.Trim();
            if (reply.Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                throw new OperationCanceledException("用户退出");
            }

            if (string.IsNullOrEmpty(reply))
            {
                Console.WriteLine("已跳过该消息");
                continue;
            }

            try
            {
                await _sender.SendTextAsync(_account, message.UserUid, reply, cancellationToken);
                Console.WriteLine("消息发送成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
            }
        }
    }

    private static string FormatTimestamp(long? timestamp)
    {
        if (timestamp is null)
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value);
        return dt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
