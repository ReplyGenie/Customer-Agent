using System.Text.Json;
using CustomerAgent.ConsoleApp.Domain.Messaging;

namespace CustomerAgent.ConsoleApp.Services;

public static class PddMessageParser
{
    public static bool TryParse(string shopId, string payload, out PddUserMessage? message, out string? warning)
    {
        message = null;
        warning = null;
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException ex)
        {
            warning = $"无法解析WebSocket消息: {ex.Message}";
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            var responseType = root.TryGetProperty("response", out var responseElement)
                ? responseElement.GetString()
                : null;

            if (responseType is null)
            {
                warning = "消息缺少response字段";
                return false;
            }

            if (responseType.Equals("auth", StringComparison.OrdinalIgnoreCase))
            {
                var context = new PddUserMessage(
                    shopId,
                    root.TryGetProperty("uid", out var uidEl) ? uidEl.GetString() ?? string.Empty : string.Empty,
                    null,
                    ContextType.Auth,
                    root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null,
                    root.Clone(),
                    null);
                message = context;
                return true;
            }

            if (responseType.Equals("mall_system_msg", StringComparison.OrdinalIgnoreCase))
            {
                var data = root.TryGetProperty("message", out var msgEl) && msgEl.TryGetProperty("data", out var dataEl)
                    ? dataEl.ToString()
                    : null;
                message = new PddUserMessage(shopId, string.Empty, null, ContextType.MallSystemMessage, data, root.Clone(), null);
                return true;
            }

            if (!responseType.Equals("push", StringComparison.OrdinalIgnoreCase))
            {
                warning = $"忽略未知response类型: {responseType}";
                return false;
            }

            if (!root.TryGetProperty("message", out var messageElement))
            {
                warning = "push消息缺少message字段";
                return false;
            }

            var fromRole = messageElement.TryGetProperty("from", out var fromElement) && fromElement.TryGetProperty("role", out var roleEl)
                ? roleEl.GetString()
                : null;

            if (string.Equals(fromRole, "mall_cs", StringComparison.OrdinalIgnoreCase))
            {
                warning = "忽略客服自己的消息";
                return false;
            }

            var fromUid = messageElement.TryGetProperty("from", out var fromObj) && fromObj.TryGetProperty("uid", out var uidElement)
                ? uidElement.GetString() ?? string.Empty
                : string.Empty;
            var nickname = messageElement.TryGetProperty("nickname", out var nicknameEl) ? nicknameEl.GetString() : null;
            var timestamp = messageElement.TryGetProperty("time", out var timeEl) && timeEl.ValueKind == JsonValueKind.Number
                ? timeEl.GetInt64()
                : (long?)null;

            ContextType contextType;
            string? text = null;

            var type = messageElement.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.Number
                ? typeElement.GetInt32()
                : -1;

            switch (type)
            {
                case 0:
                    var subType = messageElement.TryGetProperty("sub_type", out var subTypeEl) && subTypeEl.ValueKind == JsonValueKind.Number
                        ? subTypeEl.GetInt32()
                        : -1;
                    if (subType == 1)
                    {
                        contextType = ContextType.OrderInfo;
                        text = messageElement.TryGetProperty("info", out var infoEl) ? infoEl.ToString() : null;
                    }
                    else if (subType == 0)
                    {
                        contextType = ContextType.GoodsInquiry;
                        text = messageElement.TryGetProperty("info", out var infoEl) ? infoEl.ToString() : null;
                    }
                    else
                    {
                        contextType = ContextType.Text;
                        text = messageElement.TryGetProperty("content", out var contentEl) ? contentEl.GetString() : null;
                    }
                    break;
                case 1:
                    contextType = ContextType.Image;
                    text = messageElement.TryGetProperty("content", out var imgEl) ? imgEl.GetString() : null;
                    break;
                case 14:
                    contextType = ContextType.Video;
                    text = messageElement.TryGetProperty("content", out var videoEl) ? videoEl.GetString() : null;
                    break;
                case 5:
                    contextType = ContextType.Emotion;
                    text = messageElement.TryGetProperty("info", out var emotionEl) ? emotionEl.ToString() : null;
                    break;
                case 64:
                    contextType = ContextType.GoodsSpec;
                    text = messageElement.TryGetProperty("info", out var specEl) ? specEl.ToString() : null;
                    break;
                case 24:
                    contextType = ContextType.Transfer;
                    text = messageElement.TryGetProperty("info", out var transferEl) ? transferEl.ToString() : null;
                    break;
                case 1002:
                    contextType = ContextType.Withdraw;
                    text = messageElement.TryGetProperty("info", out var withdrawEl) ? withdrawEl.ToString() : null;
                    break;
                default:
                    contextType = ContextType.SystemStatus;
                    text = $"不支持的消息类型: {type}";
                    break;
            }

            message = new PddUserMessage(shopId, fromUid, nickname, contextType, text, root.Clone(), timestamp);
            return true;
        }
    }
}
