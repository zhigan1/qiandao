namespace QuarkNetCheckIn;

public sealed record QuarkAccount(string? User, string Kps, string? Sign, string? Vcode)
{
    public static IReadOnlyList<QuarkAccount> ParseAll(string raw)
    {
        var accounts = new List<QuarkAccount>();
        string[] chunks = raw
            .Replace("&&", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < chunks.Length; i++)
        {
            accounts.Add(Parse(chunks[i], i + 1));
        }

        return accounts;
    }

    private static QuarkAccount Parse(string chunk, int index)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string part in chunk.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string key = part[..eq].Trim();
            string value = part[(eq + 1)..].Trim();
            if (key.Length > 0)
            {
                fields[key] = value;
            }
        }

        if (fields.Count == 0)
        {
            throw new FormatException($"第 {index} 个账号没有解析出任何参数，请检查 COOKIE_QUARK 格式。");
        }

        if (!fields.TryGetValue("kps", out string? kps) || string.IsNullOrEmpty(kps))
        {
            throw new FormatException($"第 {index} 个账号缺少 kps 参数。");
        }

        fields.TryGetValue("user", out string? user);
        fields.TryGetValue("sign", out string? sign);
        fields.TryGetValue("vcode", out string? vcode);

        return new QuarkAccount(user, kps, sign, vcode);
    }
}
