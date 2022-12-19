using Newtonsoft.Json;
using System;
using System.IO;

public class BotConfig
{
    public string DiscordToken { get; set; } = "";
    public string WebHookUrl { get; set; } = "";
    public string ExHentaiCookieMemberId { get; set; } = "";
    public string ExHentaiCookiePassHash { get; set; } = "";
    public string ExHentaiCookieSK { get; set; } = "";
    public string GitHubApiKey { get; set; } = "";
    public string SauceNAOApiKey { get; set; } = "";
    public ulong TestSlashCommandGuildId { get; set; } = 0;

    public void InitBotConfig()
    {
        try { File.WriteAllText("bot_config_example.json", JsonConvert.SerializeObject(new BotConfig(), Formatting.Indented)); } catch { }
        if (!File.Exists("bot_config.json"))
        {
            Log.Error($"bot_config.json遺失，請依照 {Path.GetFullPath("bot_config_example.json")} 內的格式填入正確的數值");
            if (!Console.IsInputRedirected)
                Console.ReadKey();
            Environment.Exit(3);
        }

        var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("bot_config.json"));

        try
        {
            if (string.IsNullOrWhiteSpace(config.DiscordToken))
            {
                Log.Error("DiscordToken遺失，請輸入至bot_config.json後重開Bot");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.WebHookUrl))
            {
                Log.Error("WebHookUrl遺失，請輸入至bot_config.json後重開Bot");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.ExHentaiCookieMemberId))
            {
                Log.Error("ExHentaiCookieMemberId遺失，請輸入至bot_config.json後重開Bot");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.ExHentaiCookiePassHash))
            {
                Log.Error("ExHentaiCookiePassHash遺失，請輸入至bot_config.json後重開Bot");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.ExHentaiCookieSK))
            {
                Log.Error("ExHentaiCookieSK遺失，請輸入至bot_config.json後重開Bot");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.SauceNAOApiKey))
            {
                Log.Error("SauceNAOApiKey遺失，請輸入至bot_config.json後重開Bot");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.GitHubApiKey))
            {
                Log.Warn("GitHubApiKey遺失，將不使用API Key，可能會遇到Rate limlt");
                Log.Warn("如需註冊請至 https://github.com/settings/tokens 中新增 (不須設定Scope)");
            }

            DiscordToken = config.DiscordToken;
            WebHookUrl = config.WebHookUrl;
            ExHentaiCookieMemberId = config.ExHentaiCookieMemberId;
            ExHentaiCookiePassHash = config.ExHentaiCookiePassHash;
            ExHentaiCookieSK = config.ExHentaiCookieSK;
            GitHubApiKey = config.GitHubApiKey;
            SauceNAOApiKey = config.SauceNAOApiKey;
            TestSlashCommandGuildId = config.TestSlashCommandGuildId;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            throw;
        }
    }
}