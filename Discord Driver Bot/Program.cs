using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Driver_Bot.Command;
using Discord_Driver_Bot.SQLite.Table;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Driver_Bot
{
    class Program
    {
        public const string VERSION = "V1.1.0";
        
        #region 變數
        public static List<BookData> ListBookLogData { get; set; }
        public static BotConfig BotConfig { get; set; } = new();

        public static IUser ApplicatonOwner { get; private set; } = null;
        public static UpdateStatus updateStatus = UpdateStatus.Guild;
        public static List<TrustedGuild> trustedGuildList = new List<TrustedGuild>();
        public static Dictionary<string, string> eventParticipateDic = new Dictionary<string, string>();
        public static Stopwatch stopWatch = new Stopwatch();
        public static bool isConnect = false, isDisconnect = false;
        public static DiscordSocketClient _client;
        public static SQLite.DriverContext db = new SQLite.DriverContext();
        static Timer timerUpdateStatus, timerCheckTranUpdate;

        public enum UpdateStatus { Guild, Member, ShowBook, Info, ReadBook }

        static GitHubClient gitHubClient = new GitHubClient(new ProductHeaderValue("Discord_Driver_Bot"));
        #endregion

        static void Main(string[] args)
        {
            #region 初始化
            Log.FormatColorWrite(VERSION + " 初始化中", ConsoleColor.DarkYellow);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += Console_CancelKeyPress;

            BotConfig.InitBotConfig();

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
                trustedGuildList = db.TrustedGuild.ToList();
            }

            Log.FormatColorWrite("初始化資料庫完成!", ConsoleColor.DarkYellow);

            try { Book.Host.EHentai.API.GetExHentaiData("https://e-hentai.org/g/1586147/c51e3aae3c/"); }
            catch (Exception ex) { if (!ex.Message.Contains("404")) throw; }

            Log.FormatColorWrite("ExHentai Cookie初始化完成!", ConsoleColor.DarkYellow);

            new Program().MainAsync().GetAwaiter().GetResult();
            #endregion
        }
        
        private static void TimerHandler(object state)
        {
            try
            {
                using (var db = new SQLite.DriverContext())
                {
                    Release release = gitHubClient.Repository.Release.GetLatest("EhTagTranslation", "Database").Result; //https://github.com/EhTagTranslation/Database/releases
                    DbBotConfig dateBase = db.DbBotConfig.First();
                    long latest = release.CreatedAt.ToUnixTimeMilliseconds();

                    if (dateBase.TagTranslationCreatedAt < latest)
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                webClient.DownloadFile(release.Assets.First((x) => x.Name == "db.raw.json").BrowserDownloadUrl, GetDataFilePath("db.raw.json.tmp"));
                                try
                                {
                                    Process.Start("opencc", $"-i \"{GetDataFilePath("db.raw.json.tmp")}\" -o \"{GetDataFilePath("db.raw.json")}\" -c s2tw").WaitForExit();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.Message);
                                }
                              
                                if (File.Exists(GetDataFilePath("db.raw.json")))
                                {
                                    File.Delete(GetDataFilePath("db.raw.json.tmp"));
                                    Log.FormatColorWrite($"Ex標籤更新完成\n`{release.Name}`", ConsoleColor.DarkCyan);
                                }
                                else
                                {
                                    File.Move(GetDataFilePath("db.raw.json.tmp"), GetDataFilePath("db.raw.json"));
                                    SendMessageToDiscord("Ex標籤翻譯失敗");
                                }

                                dateBase.TagTranslationCreatedAt = latest;
                                db.DbBotConfig.Update(dateBase);
                                db.SaveChanges();
                            }
                            else
                            {
                                webClient.DownloadFile(release.Assets.First((x) => x.Name == "db.raw.json").BrowserDownloadUrl, GetDataFilePath("db.raw.json"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendMessageToDiscord($"更新Ex標籤失敗\n{ex.Message}");
            }

            if (File.Exists(GetDataFilePath("db.raw.json")))
            {
                Book.Host.EHentai.TagTranslation.TranslationData =
                    JsonConvert.DeserializeObject<Book.Host.EHentai.TagTranslation.DataBase>(File.ReadAllText(GetDataFilePath("db.raw.json"))).Data;
            }
            else
            {
                SendMessageToDiscord("更新Ex標籤失敗");
            }
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Warning,
                ConnectionTimeout = int.MaxValue,
                MessageCacheSize = 50,
                ExclusiveBulkDelete = true
            }); ;

            _client.Ready += () =>
            {
                stopWatch.Start();
                timerUpdateStatus.Change(0, 15 * 60 * 1000);
                timerCheckTranUpdate.Change(0, 60 * 60 * 1000);

                ApplicatonOwner = _client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner;
                isConnect = true;

                //Log.FormatColorWrite("準備完成", ConsoleColor.Green);

                return Task.CompletedTask;
            };

            _client.JoinedGuild += (guild) =>
            {
                SendMessageToDiscord($"加入 {guild.Name}({guild.Id})\n擁有者: {guild.Owner.Username}({guild.Owner.Mention})");
                return Task.CompletedTask;
            };

            #region 初始化指令系統
            var s = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(BotConfig)
                .AddSingleton(new CommandService(new CommandServiceConfig()
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = RunMode.Async
                }));

            s.LoadFrom(Assembly.GetAssembly(typeof(CommandHandler)));
            IServiceProvider service = s.BuildServiceProvider();
            await service.GetService<CommandHandler>().InitializeAsync();
            #endregion

            #region Login
            await _client.LoginAsync(TokenType.Bot, BotConfig.DiscordToken);
            #endregion

            await _client.StartAsync();

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
            Action<string> setGame = new Action<string>((string text) => { _client.SetGameAsync($"!!h | {text}"); });

            switch (updateStatus)
            {
                case UpdateStatus.Guild:
                    setGame($"在 {_client.Guilds.Count} 個伺服器");
                    updateStatus = UpdateStatus.Member;
                    break;
                case UpdateStatus.Member:
                    try
                    {
                        int totleMemberCount = 0;
                        foreach (var item in _client.Guilds) totleMemberCount += item.MemberCount;
                        setGame($"服務 {totleMemberCount} 個成員");
                        updateStatus = UpdateStatus.ShowBook;
                    }
                    catch (Exception) { updateStatus = UpdateStatus.ShowBook; ChangeStatus(); }
                    break;
                case UpdateStatus.ShowBook:
                    setGame($"看了 {ListBookLogData.Count} 本本子");
                    updateStatus = UpdateStatus.Info;
                    break;
                case UpdateStatus.Info:
                    setGame("去看你的本本啦");
                    updateStatus = UpdateStatus.ReadBook;
                    break;
                case UpdateStatus.ReadBook:
                    BookData bookData = ListBookLogData[new Random().Next(0, ListBookLogData.Count)];
                    setGame(bookData.Title + "\n" + bookData.URL.Replace("https://", ""));
                    updateStatus = UpdateStatus.Guild;
                    break;

            }
        }

        public static string GetDataFilePath(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Data" +
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/") + fileName;
        }

        public static void SendMessageToDiscord(string content)
        {
            Message message = new Message();

            if (isConnect) message.username = _client.CurrentUser.Username;
            else message.username = "Bot";

            if (isConnect) message.avatar_url = _client.CurrentUser.GetAvatarUrl();
            else message.avatar_url = "";

            message.content = content;

            using (WebClient webClient = new WebClient())
            {
                webClient.Encoding = System.Text.Encoding.UTF8;
                webClient.Headers["Content-Type"] = "application/json";
                webClient.UploadString(BotConfig.WebHookUrl, JsonConvert.SerializeObject(message));
            }
        }

        public class Message
        {
            public string username { get; set; }
            public string content { get; set; }
            public string avatar_url { get; set; }
        }
    }
}