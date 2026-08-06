using System.Text;
using QuarkNetCheckIn;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("---------- 夸克网盘自动签到 (.NET) ----------");

string? raw = Environment.GetEnvironmentVariable("COOKIE_QUARK");
if (string.IsNullOrWhiteSpace(raw))
{
    Console.WriteLine("未添加 COOKIE_QUARK 变量，请先配置环境变量后再运行。");
    return 1;
}

IReadOnlyList<QuarkAccount> accounts;
try
{
    accounts = QuarkAccount.ParseAll(raw);
}
catch (FormatException ex)
{
    Console.WriteLine($"COOKIE_QUARK 格式错误: {ex.Message}");
    return 1;
}

if (accounts.Count == 0)
{
    Console.WriteLine("没有解析到有效的账号信息。");
    return 1;
}

Console.WriteLine($"检测到 {accounts.Count} 个夸克账号");

bool hasError = false;
for (int i = 0; i < accounts.Count; i++)
{
    Console.WriteLine();
    Console.WriteLine($"===== 账号 {i + 1}/{accounts.Count} =====");

    try
    {
        string result = await QuarkClient.SignInAsync(accounts[i]);
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        hasError = true;
        Console.WriteLine($"签到异常: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("---------- 夸克网盘签到完毕 ----------");
return hasError ? 1 : 0;
