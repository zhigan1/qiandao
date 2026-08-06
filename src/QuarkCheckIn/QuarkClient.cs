using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace QuarkNetCheckIn;

public static class QuarkClient
{
    private const string GrowthInfoUrl = "https://drive-m.quark.cn/1/clouddrive/capacity/growth/info";
    private const string GrowthSignUrl = "https://drive-m.quark.cn/1/clouddrive/capacity/growth/sign";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    public static async Task<string> SignInAsync(QuarkAccount account, CancellationToken cancellationToken = default)
    {
        JsonNode? growth = await GetGrowthInfoAsync(account, cancellationToken);
        if (growth is null)
        {
            throw new QuarkException("获取成长信息失败，请检查 COOKIE_QUARK 是否有效。");
        }

        var builder = new StringBuilder();
        string userLabel = account.User ?? "未知用户";
        string vipLabel = ReadBool(growth, "88VIP") ? "88VIP" : "普通用户";
        long totalCapacity = ReadLong(growth, "total_capacity") ?? 0;
        long signReward = ReadLong(growth, "cap_composition", "sign_reward") ?? 0;

        builder.AppendLine($"[{vipLabel}] {userLabel}");
        builder.AppendLine($"网盘总容量: {FormatBytes(totalCapacity)}");
        builder.AppendLine($"签到累计容量: {FormatBytes(signReward)}");

        JsonNode? capSign = growth["cap_sign"];
        bool signedToday = ReadBool(capSign, "sign_daily");
        int progress = (int)(ReadLong(capSign, "sign_progress") ?? 0);
        int target = (int)(ReadLong(capSign, "sign_target") ?? 0);

        if (signedToday)
        {
            long reward = ReadLong(capSign, "sign_daily_reward") ?? 0;
            builder.AppendLine($"签到日志: 今日已签到，获得 {FormatBytes(reward)}，连签进度 ({progress}/{target})");
        }
        else
        {
            JsonNode? sign = await SignAsync(account, cancellationToken);
            if (sign is null)
            {
                throw new QuarkException("执行签到失败，请检查 COOKIE_QUARK 是否有效。");
            }

            long reward = ReadLong(sign, "sign_daily_reward") ?? 0;
            builder.AppendLine($"执行签到: 今日签到 +{FormatBytes(reward)}，连签进度 ({progress + 1}/{target})");
        }

        return builder.ToString().TrimEnd();
    }

    private static async Task<JsonNode?> GetGrowthInfoAsync(QuarkAccount account, CancellationToken ct)
    {
        Uri url = BuildUrl(GrowthInfoUrl, account);
        JsonNode? root = await GetJsonAsync(url, ct);
        return root?["data"];
    }

    private static async Task<JsonNode?> SignAsync(QuarkAccount account, CancellationToken ct)
    {
        Uri url = BuildUrl(GrowthSignUrl, account);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { sign_cyclic = true }),
        };

        JsonNode? root = await GetJsonAsync(request, ct);
        return root?["data"];
    }

    private static async Task<JsonNode?> GetJsonAsync(Uri url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await GetJsonAsync(request, ct);
    }

    private static async Task<JsonNode?> GetJsonAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using HttpResponseMessage response = await Http.SendAsync(request, ct);
        string body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new QuarkException($"HTTP {(int)response.StatusCode}: {Truncate(body)}");
        }

        var root = JsonNode.Parse(body) as JsonObject;
        if (root is null)
        {
            throw new QuarkException($"接口返回不是有效 JSON: {Truncate(body)}");
        }

        long? status = ReadLong(root, "status");
        if (status is not null && status != 200 && root["data"] is null)
        {
            string message = root["message"]?.ToString()
                ?? root["msg"]?.ToString()
                ?? "未知错误";
            throw new QuarkException($"接口返回错误: {message}");
        }

        return root;
    }

    private static Uri BuildUrl(string baseUrl, QuarkAccount account)
    {
        var parameters = new Dictionary<string, string>
        {
            ["pr"] = "ucpro",
            ["fr"] = "android",
            ["kps"] = account.Kps,
        };

        if (!string.IsNullOrEmpty(account.Sign))
        {
            parameters["sign"] = account.Sign;
        }

        if (!string.IsNullOrEmpty(account.Vcode))
        {
            parameters["vcode"] = account.Vcode;
        }

        string query = string.Join("&", parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return new Uri($"{baseUrl}?{query}");
    }

    private static bool ReadBool(JsonNode? node, string key) => node?[key]?.GetValue<bool>() ?? false;

    private static long? ReadLong(JsonNode? node, params string[] path)
    {
        JsonNode? current = node;
        foreach (string key in path)
        {
            current = current?[key];
            if (current is null)
            {
                return null;
            }
        }

        return current?.GetValue<long>();
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
        double value = bytes;
        int i = 0;

        while (value >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }

        return $"{value.ToString("0.00", CultureInfo.InvariantCulture)} {units[i]}";
    }

    private static string Truncate(string text, int maxLength = 200) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}

public sealed class QuarkException : Exception
{
    public QuarkException(string message) : base(message)
    {
    }
}
