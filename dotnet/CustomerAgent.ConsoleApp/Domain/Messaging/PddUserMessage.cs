using System.Text.Json;

namespace CustomerAgent.ConsoleApp.Domain.Messaging;

public sealed record PddUserMessage(
    string ShopId,
    string UserUid,
    string? Nickname,
    ContextType ContextType,
    string? Text,
    JsonElement RawMessage,
    long? Timestamp
);
