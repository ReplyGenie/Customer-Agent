using System.Linq;
using System.Threading.Channels;
using CustomerAgent.ConsoleApp.Configuration;
using CustomerAgent.ConsoleApp.Domain.Entities;
using CustomerAgent.ConsoleApp.Domain.Messaging;
using CustomerAgent.ConsoleApp.Infrastructure.Http;
using CustomerAgent.ConsoleApp.Infrastructure.Persistence;
using CustomerAgent.ConsoleApp.Infrastructure.WebSockets;
using CustomerAgent.ConsoleApp.Services;

var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
AppSettings settings;
try
{
    settings = ConfigurationLoader.Load(configPath);
}
catch (Exception ex)
{
    Console.WriteLine($"加载配置失败: {ex.Message}");
    return;
}

Console.WriteLine("=== 拼多多客服控制台 ===");
Console.WriteLine("请输入账号信息，按回车确认");

Console.Write("账号: ");
var username = Console.ReadLine() ?? string.Empty;
Console.Write("密码: ");
var password = ReadPassword();
Console.WriteLine();
Console.WriteLine("请输入Cookie(支持JSON或key=value;形式):");
var cookieInput = Console.ReadLine() ?? string.Empty;

var cookies = CookieUtility.Parse(cookieInput);
if (cookies.Count == 0)
{
    Console.WriteLine("未解析到有效Cookie，程序结束");
    return;
}

var database = new InMemoryDatabase();
var account = new Account
{
    Username = username,
    Password = password
};
account.UpdateCookies(cookies);
database.UpsertAccount(account);

var accountService = new PddAccountService(settings);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    Console.WriteLine("正在获取用户信息...");
    var (userId, accountName, mallId) = await accountService.GetUserInfoAsync(account, cts.Token);
    account.UserId = userId;
    account.Username = accountName;
    account.ShopId = mallId;
    Console.WriteLine($"用户ID: {userId}, 昵称: {accountName}, 关联店铺ID: {mallId}");

    Console.WriteLine("正在获取店铺信息...");
    var (shopId, shopName, mallLogo) = await accountService.GetShopInfoAsync(account, cts.Token);
    account.ShopId = shopId;
    account.ShopName = shopName;
    account.MallLogo = mallLogo;
    database.SaveShop(new Shop { ShopId = shopId, Name = shopName, Logo = mallLogo });
    Console.WriteLine($"店铺: {shopName} ({shopId})");

    Console.WriteLine("正在获取token...");
    var token = await accountService.GetTokenAsync(account, cts.Token);
    Console.WriteLine("Token获取成功，准备连接WebSocket...");

    var channel = Channel.CreateUnbounded<PddUserMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    var messageSender = new PddMessageSender(settings);
    var dispatcher = new PddMessageDispatcher(channel, messageSender, account);
    var websocketClient = new PddWebSocketClient(channel);

    var dispatcherTask = Task.Run(() => dispatcher.RunAsync(cts.Token), cts.Token);
    var websocketTask = Task.Run(() => websocketClient.RunAsync(account, token, cts.Token), cts.Token);

    Console.WriteLine("连接已建立，等待用户消息，按Ctrl+C退出");

    await Task.WhenAll(dispatcherTask, websocketTask);
}
catch (OperationCanceledException)
{
    Console.WriteLine("用户取消，正在退出...");
}
catch (Exception ex)
{
    Console.WriteLine($"发生错误: {ex.Message}");
}

static string ReadPassword()
{
    var password = new Stack<char>();
    ConsoleKeyInfo keyInfo;
    while (true)
    {
        keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.Key == ConsoleKey.Enter)
        {
            break;
        }
        if (keyInfo.Key == ConsoleKey.Backspace)
        {
            if (password.Count > 0)
            {
                password.Pop();
                Console.Write("\b \b");
            }
            continue;
        }
        password.Push(keyInfo.KeyChar);
        Console.Write('*');
    }

    return new string(password.Reverse().ToArray());
}
