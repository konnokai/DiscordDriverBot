﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordDriverBot.Command;
using DiscordDriverBot.HttpClients;
using DiscordDriverBot.HttpClients.Ascii2D;
using DiscordDriverBot.HttpClients.SauceNAO;
using DiscordDriverBot.Interaction;
using DiscordDriverBot.SQLite.Table;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordDriverBot
{
    class Program
    {
        public static string VERSION => GetLinkerTime(Assembly.GetEntryAssembly());

        #region 變數
        public static List<BookData> ListBookLogData { get; set; }
        public static BotConfig BotConfig { get; set; } = new();
        public static EHentaiAPIClient EHentaiAPIClient { get; set; }
        public static HttpClients.NHentai.NHentaiAPIClient NHentaiAPIClient { get; set; } = new();
        public static HttpClients.Hitomi.HitomiAPIClient HitomiAPIClient { get; set; } = new();

        public static IUser ApplicatonOwner { get; private set; } = null;
        public static UpdateStatus updateStatus = UpdateStatus.Guild;
        public static Dictionary<string, string> eventParticipateDic = new Dictionary<string, string>();
        public static Stopwatch stopWatch = new Stopwatch();
        public static bool isConnect = false, isDisconnect = false;
        public static DiscordSocketClient _client;
        public static SQLite.DriverContext db = new SQLite.DriverContext();
        static Timer timerUpdateStatus, timerCheckTranUpdate;
        static IServiceProvider iService = null;

        public enum UpdateStatus { Guild, Member, ShowBook, Info, ReadBook }

        static GitHubClient gitHubClient = new GitHubClient(new ProductHeaderValue("DiscordDriverBot"));
        #endregion

        static void Main(string[] args)
        {
            #region 初始化
            Log.Info(VERSION + " 初始化中");
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += Console_CancelKeyPress;

            BotConfig.InitBotConfig();

            if (!string.IsNullOrEmpty(BotConfig.GitHubApiKey))
                gitHubClient.Credentials = new Credentials(BotConfig.GitHubApiKey);
            EHentaiAPIClient = new EHentaiAPIClient();

            timerUpdateStatus = new Timer((state) => ChangeStatus());
            timerCheckTranUpdate = new Timer(TimerHandler);

            if (!Directory.Exists(Path.GetDirectoryName(GetDataFilePath(""))))
                Directory.CreateDirectory(Path.GetDirectoryName(GetDataFilePath("")));

            using (var db = new SQLite.DriverContext())
            {
                if (!File.Exists(GetDataFilePath("DataBase.db")))
                {
                    db.Database.EnsureCreated();
                    db.DbBotConfig.Add(new DbBotConfig() { TagTranslationCreatedAt = 1 });
                    db.SaveChanges();
                }

                ListBookLogData = db.BookData.ToList();
            }

            Log.Info("初始化資料庫完成!");

            try { _ = EHentaiAPIClient.GetExHentaiDataAsync("https://e-hentai.org/g/1586147/c51e3aae3c/"); }
            catch (Exception ex) { if (!ex.Message.Contains("404")) throw; }

            Log.Info("ExHentai Cookie初始化完成!");

            new Program().MainAsync().GetAwaiter().GetResult();
            #endregion
        }

        private static void TimerHandler(object state)
        {
            try
            {
                using (var db = new SQLite.DriverContext())
                {
                    var release = gitHubClient.Repository.Release.GetLatest("EhTagTranslation", "Database").Result; //https://github.com/EhTagTranslation/DatabaseReleases/releases
                    var latest = release.CreatedAt.ToUnixTimeMilliseconds();
                    DbBotConfig dateBase = db.DbBotConfig.First();

                    if (dateBase.TagTranslationCreatedAt < latest)
                    {
                        var commit = gitHubClient.Repository.Commit.Get("EhTagTranslation", "DatabaseReleases", "master").Result; //https://github.com/EhTagTranslation/DatabaseReleases/releases
                        var file = commit.Files.FirstOrDefault((x) => x.Filename == "db.raw.json");
                        if (file == null) return;

                        var data = iService.GetService<HttpClient>().GetByteArrayAsync(file.RawUrl).Result;
                        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                        File.WriteAllBytes(GetDataFilePath(isLinux ? "db.raw.json.tmp" : "db.raw.json"), data);

                        if (isLinux)
                        {
                            try
                            {
                                Process.Start("opencc", $"-i \"{GetDataFilePath("db.raw.json.tmp")}\" -o \"{GetDataFilePath("db.raw.json")}\" -c s2tw").WaitForExit();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }

                            if (File.Exists(GetDataFilePath("db.raw.json")))
                            {
                                File.Delete(GetDataFilePath("db.raw.json.tmp"));
                                Log.FormatColorWrite($"Ex標籤更新完成: `{release.Name}`", ConsoleColor.DarkCyan);
                            }
                            else
                            {
                                File.Move(GetDataFilePath("db.raw.json.tmp"), GetDataFilePath("db.raw.json"));
                                iService.GetService<DiscordWebhookClient>().SendMessageToDiscord("Ex標籤翻譯失敗");
                            }
                        }

                        dateBase.TagTranslationCreatedAt = latest;
                        db.DbBotConfig.Update(dateBase);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                iService.GetService<DiscordWebhookClient>().SendMessageToDiscord($"更新Ex標籤失敗\n{ex}");
            }

            if (File.Exists(GetDataFilePath("db.raw.json")))
            {
                Gallery.Host.EHentai.TagTranslation.TranslationData =
                    JsonConvert.DeserializeObject<Gallery.Host.EHentai.TagTranslation.DataBase>(File.ReadAllText(GetDataFilePath("db.raw.json"))).Data;
            }
            else
            {
                iService.GetService<DiscordWebhookClient>().SendMessageToDiscord("更新Ex標籤失敗");
            }
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                ConnectionTimeout = int.MaxValue,
                MessageCacheSize = 50,
                GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents,
                AlwaysDownloadDefaultStickers = false,
                AlwaysResolveStickers = false,
                FormatUsersInBidirectionalUnicode = false,
                LogGatewayIntentWarnings = false,
            }); ;

            #region 初始化Discord設定與事件
            _client.Log += Log.LogMsg;

            _client.Ready += async () =>
            {
                stopWatch.Start();
                timerUpdateStatus.Change(0, 15 * 60 * 1000);
                timerCheckTranUpdate.Change(0, 60 * 60 * 1000);

                ApplicatonOwner = (await _client.GetApplicationInfoAsync()).Owner;
                isConnect = true;
            };

            _client.JoinedGuild += (guild) =>
            {
                iService.GetService<DiscordWebhookClient>().SendMessageToDiscord($"加入 {guild.Name}({guild.Id})\n擁有者: {guild.OwnerId}");
                return Task.CompletedTask;
            };
            #endregion

            Log.Info("登入中...");
            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            await _client.StartAsync();

            do { await Task.Delay(200); }
            while (!isConnect);

            Log.Info("登入成功!");

            UptimeKumaClient.Init(BotConfig.UptimeKumaPushUrl, _client);

            #region 初始化互動指令系統
            var interactionServices = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(_client)
                .AddSingleton(BotConfig)
                .AddSingleton(new InteractionService(_client, new InteractionServiceConfig()
                {
                    AutoServiceScopes = true,
                    UseCompiledLambda = true,
                    EnableAutocompleteHandlers = false,
                    DefaultRunMode = Discord.Interactions.RunMode.Async
                }));

            interactionServices.AddHttpClient<Ascii2DClient>();
            interactionServices.AddHttpClient<DiscordWebhookClient>();
            interactionServices.AddHttpClient<SauceNAOClient>();
            interactionServices.AddHttpClient();

            interactionServices.LoadInteractionFrom(Assembly.GetAssembly(typeof(InteractionHandler)));
            iService = interactionServices.BuildServiceProvider();
            await iService.GetService<InteractionHandler>().InitializeAsync();
            #endregion

            #region 初始化一般指令系統
            var commandServices = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(_client)
                .AddSingleton(BotConfig)
                .AddSingleton(new CommandService(new CommandServiceConfig()
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = Discord.Commands.RunMode.Async
                }));

            commandServices.AddHttpClient<Ascii2DClient>();
            commandServices.AddHttpClient<DiscordWebhookClient>();
            commandServices.AddHttpClient<SauceNAOClient>();
            commandServices.AddHttpClient();

            commandServices.LoadCommandFrom(Assembly.GetAssembly(typeof(CommandHandler)));
            IServiceProvider service = commandServices.BuildServiceProvider();
            await service.GetService<CommandHandler>().InitializeAsync();
            #endregion

            #region 註冊互動指令
            try
            {
                InteractionService interactionService = iService.GetService<InteractionService>();
#if DEBUG
                if (BotConfig.TestSlashCommandGuildId == 0 || _client.GetGuild(BotConfig.TestSlashCommandGuildId) == null)
                    Log.Warn("未設定測試Slash指令的伺服器或伺服器不存在，略過");
                else
                {
                    try
                    {
                        var result = await interactionService.RegisterCommandsToGuildAsync(BotConfig.TestSlashCommandGuildId);
                        Log.Info($"已註冊指令 ({BotConfig.TestSlashCommandGuildId}) : {string.Join(", ", result.Select((x) => x.Name))}");

                        result = await interactionService.AddModulesToGuildAsync(BotConfig.TestSlashCommandGuildId, false, interactionService.Modules.Where((x) => x.DontAutoRegister).ToArray());
                        Log.Info($"已註冊指令 ({BotConfig.TestSlashCommandGuildId}) : {string.Join(", ", result.Select((x) => x.Name))}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("註冊伺服器專用Slash指令失敗");
                        Log.Error(ex.ToString());
                    }
                }
#else
                try
                {
                    try
                    {
                        int commandCount = 0;

                        if (File.Exists(GetDataFilePath("CommandCount.bin")))
                            commandCount = BitConverter.ToInt32(File.ReadAllBytes(GetDataFilePath("CommandCount.bin")));

                        if (BotConfig.TestSlashCommandGuildId != 0 && _client.GetGuild(BotConfig.TestSlashCommandGuildId) != null)
                        {
                            var result = await interactionService.RemoveModulesFromGuildAsync(BotConfig.TestSlashCommandGuildId, interactionService.Modules.Where((x) => !x.DontAutoRegister).ToArray());
                            Log.Info($"({BotConfig.TestSlashCommandGuildId}) 已移除測試指令，剩餘指令: {string.Join(", ", result.Select((x) => x.Name))}");
                        }

                        if (commandCount != iService.GetService<InteractionHandler>().CommandCount)
                        {
                            try
                            {
                                foreach (var item in interactionService.Modules.Where((x) => x.Preconditions.Any((x) => x is Interaction.Attribute.RequireGuildAttribute)))
                                {
                                    var guildId = ((Interaction.Attribute.RequireGuildAttribute)item.Preconditions.FirstOrDefault((x) => x is Interaction.Attribute.RequireGuildAttribute)).GuildId;
                                    var guild = _client.GetGuild(guildId.Value);

                                    if (guild == null)
                                    {
                                        Log.Warn($"{item.Name} 註冊失敗，伺服器 {guildId} 不存在");
                                        continue;
                                    }

                                    var result = await interactionService.AddModulesToGuildAsync(guild, false, item);
                                    Log.Info($"已在 {guild.Name}({guild.Id}) 註冊指令: {string.Join(", ", item.SlashCommands.Select((x) => x.Name))}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "註冊伺服器專用Slash指令失敗");
                            }

                            await iService.GetService<InteractionService>().RegisterCommandsGloballyAsync();
                            File.WriteAllBytes(GetDataFilePath("CommandCount.bin"), BitConverter.GetBytes(iService.GetService<InteractionHandler>().CommandCount));
                            Log.Info("已註冊全球指令");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "設定指令數量失敗，請確認檔案是否正常");
                        if (File.Exists(GetDataFilePath("CommandCount.bin")))
                            File.Delete(GetDataFilePath("CommandCount.bin"));

                        isDisconnect = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "取得指令數量失敗");
                    isDisconnect = true;
                }
#endif
            }
            catch (Exception ex)
            {
                Log.Error("註冊Slash指令失敗，關閉中...");
                Log.Error(ex.ToString());
                isDisconnect = true;
            }
            #endregion

            Log.Info("已初始化完成!");

            do { await Task.Delay(1000); }
            while (!isDisconnect);


            await _client.StopAsync();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            isDisconnect = true;
            e.Cancel = true;
        }

        public static void ChangeStatus()
        {
            Task.Run(async () =>
            {
                switch (updateStatus)
                {
                    case UpdateStatus.Guild:
                        await _client.SetCustomStatusAsync($"在 {_client.Guilds.Count} 個伺服器");
                        updateStatus = UpdateStatus.Member;
                        break;
                    case UpdateStatus.Member:
                        try
                        {
                            int totleMemberCount = 0;
                            foreach (var item in _client.Guilds) totleMemberCount += item.MemberCount;
                            await _client.SetCustomStatusAsync($"服務 {totleMemberCount} 個成員");
                            updateStatus = UpdateStatus.ShowBook;
                        }
                        catch (Exception) { updateStatus = UpdateStatus.ShowBook; ChangeStatus(); }
                        break;
                    case UpdateStatus.ShowBook:
                        await _client.SetCustomStatusAsync($"看了 {ListBookLogData.Count} 本本子");
                        updateStatus = UpdateStatus.Info;
                        break;
                    case UpdateStatus.Info:
                        await _client.SetCustomStatusAsync("去看你的本本啦");
                        updateStatus = UpdateStatus.ReadBook;
                        break;
                    case UpdateStatus.ReadBook:
                        BookData bookData = ListBookLogData[new Random().Next(0, ListBookLogData.Count)];
                        await _client.SetCustomStatusAsync(bookData.Title + "\n" + bookData.URL.Replace("https://", ""));
                        updateStatus = UpdateStatus.Guild;
                        break;
                }
            });
        }

        public static string GetDataFilePath(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Data" +
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/") + fileName;
        }

        public static string GetLinkerTime(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value[(index + BuildVersionMetadataPrefix.Length)..];
                    return value;
                }
            }
            return default;
        }
    }
}