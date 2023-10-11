using Discord_Driver_Bot;
using Newtonsoft.Json;
using System;
using System.IO;

#nullable enable

public class BotConfig
{
    public string? DiscordToken { get; set; } = default;
    public string? WebHookUrl { get; set; } = default;
    public string? ExHentaiCookieMemberId { get; set; } = default;
    public string? ExHentaiCookiePassHash { get; set; } = default;
    public string? ExHentaiCookieSK { get; set; } = default;
    public string? SauceNAOApiKey { get; set; } = default;

    [NotRequirement]
    public string? GitHubApiKey { get; set; } = default;

    [NotRequirement]
    public ulong TestSlashCommandGuildId { get; set; } = 0;

    [NotRequirement]
    public string? UptimeKumaPushUrl { get; set; } = default;

    public void InitBotConfig()
    {
        if (Utility.InDocker)
        {
            Log.Info("從環境變數讀取設定");

            foreach (var item in GetType().GetProperties())
            {
                bool exitIfNoVar = false;
                object? origValue = item.GetValue(this);
                if (origValue == default && item.GetCustomAttributes(typeof(NotRequirementAttribute), false).Length == 0) exitIfNoVar = true;

                object? setValue = Utility.GetEnvironmentVariable(item.Name, item.PropertyType, exitIfNoVar);
                setValue ??= origValue;

                item.SetValue(this, setValue);
            }
        }
        else
        {
            try { File.WriteAllText("bot_config_example.json", JsonConvert.SerializeObject(new BotConfig(), Formatting.Indented)); } catch { }
            if (!File.Exists("bot_config.json"))
            {
                Log.Error($"bot_config.json遺失，請依照 {Path.GetFullPath("bot_config_example.json")} 內的格式填入正確的數值");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("bot_config.json"))!;

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
                UptimeKumaPushUrl = config.UptimeKumaPushUrl;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }
        }
    }


    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class NotRequirementAttribute : Attribute
    { }
}