using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Driver_Bot.Book.Host.EHentai;
using Discord_Driver_Bot.SauceNAOAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Command.Normal
{
    public class Normal : TopLevelModule<NormalService>
    {
        private readonly DiscordSocketClient _client;
        private readonly NormalService _normal;
        private readonly SauceNAO wrapper;
        private string[] AllowedFileTypes { get; } = new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".svg", ".webp" };

        public Normal(DiscordSocketClient client, NormalService normal,BotConfig botConfig)
        {
            _client = client;
            _normal = normal;
            wrapper = new SauceNAO(botConfig.SauceNAOApiKey);
        }

        [Command("AutoGodSay")]
        [Summary("依據輸入的字數來判斷網站並顯示神所說的句子" +
            "\n字數自動判斷的網站如下" +
            "\n5為Wnacg，6為NHentai，7為Hitomi" +
            "\n8為Pixiv，18跟19為ExHentai" +
            "\n\n例:" +
            "\n!!AutoGodSay 260600" +
            "\n!!AutoGodSay 74861170" +
            "\n!!AutoGodSay 1496326/aa30f4bfae" +
            "\n\n可以同時輸入多組語言來執行，請用\" \"分隔，單次執行最多五本")]
        [Alias("AGS")]
        public async Task AutoGodSayAsync([Summary("神的語言")][Remainder]string godSay = null)
        {
            if (godSay == null) { await ReplyAsync(Context.User.Mention + " 沒有神的語言"); return; }

            string[] list = godSay.Split(new char[] { ' ' });
            if (list.Length > 5 && Context.Message.Author != Program.ApplicatonOwner)
            { await ReplyAsync(Context.User.Mention + " 超過五本"); return; }

            foreach (var item in list)
            {
                string host;

                switch (item.Length)
                {
                    case 5:
                        host = "w";
                        break;
                    case 6:
                        host = "n";
                        break;
                    case 7:
                        host = "h";
                        break;
                    case 8:
                        host = "p";
                        break;
                    case 18:
                    case 19:
                        host = "ex";
                        break;
                    default:
                        await ReplyAsync($"{Context.User.Mention} 我不知道 {item} 所代表的網站，請使用 `!!godsay 本子網址縮寫 神的語言`");
                        return;
                }
                
                if (host != "")
                {
                    await GodSayAsync(host, item);
                }
            }
        }

        [Command("GodSay")]
        [Summary("顯示神所說的文字\n縮寫為p (普通頻道可用)\nn, w, e-, ex, h (限NSFW頻道使用)\n例:\n!!GodSay n 260600\n!!GodSay ex 1496326/aa30f4bfae")]
        [Alias("GS")]
        public async Task GodSayAsync([Summary("網站")]string host = null, [Summary("神的語言")]string godSay = null)
        {
            if (host == null) { await ReplyAsync(Context.User.Mention + " 網站錯誤"); return; }
            if (godSay == null) { await ReplyAsync(Context.User.Mention + " 沒有神的語言"); return; }

            string url;

            switch (host.ToLower())
            {
                case "n":
                    url = string.Format("https://nhentai.net/g/{0}", godSay);
                    break;
                case "w":
                    url = string.Format("https://www.wnacg.org/photos-index-aid-{0}.html", godSay);
                    break;
                case "e-":
                case "ex":
                    url = string.Format("https://exhentai.org/g/{0}", godSay);
                    break;
                case "h":
                    url = string.Format("https://hitomi.la/galleries/{0}.html", godSay);
                    break;
                case "p":
                    url = string.Format("https://www.pixiv.net/artworks/{0}", godSay);
                    break;
                default:
                    await ReplyAsync(string.Format("{1} 找不到 {0} 的縮寫網站", host, Context.User.Mention));
                    return;
            }

            if (url != "" && Book.Function.ShowBookInfo(url, Context))
            {
                SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
            }
        }

        [Command("Ping")]
        [Summary("延遲檢測")]
        public async Task PingAsync()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithDescription(":ping_pong: " + _client.Latency.ToString() + "ms");
            await ReplyAsync(null, false , embedBuilder.Build());
        }

        [Command("Search")]
        [Summary("查本本，可搜尋的網站有ex, n" +
            "\n預設搜尋ExHentai" +
            "\n如關鍵字有空白，請在關鍵字錢後加上\"\"" +
            "\n\n例:" +
            "\n!!Search \"空色れん\" 1 ex")]
        [Alias("S")]
        public async Task SearchAsync([Summary("本子關鍵字")]string keyWord = null, [Summary("頁數")]int page = 1, [Summary("搜尋網站")]string host = "ex")
        {
            if (keyWord == null) { await ReplyAsync("缺少本子關鍵字，你以為我會通靈嗎"); return; }

            switch (host)
            {
                case "n":
                    await _normal.SearchNHentai(Context, keyWord, page);
                    break;
                case "ex":
                case "e-":
                    await _normal.SearchExHentai(Context, keyWord, page--);
                    break;
                default:
                    await ReplyAsync($"我不知道 {host}");
                    break;
            }
        }

        [Command("Invite")]
        [Summary("取得邀請連結")]
        public async Task InviteAsync()
        {
#if RELEASE
            if (Context.User.Id != Program.ApplicatonOwner.Id)
            {
                Program.SendMessageToDiscord(string.Format("[{0}-{1}] {2}:({3}) 使用了邀請指令",
                    Context.Guild.Name, Context.Channel.Name, Context.Message.Author.Username, Context.Message.Author.Id));
            }
#endif

            try
            {
                await (await Context.Message.Author.GetOrCreateDMChannelAsync()).SendMessageAsync(
                    "<https://discordapp.com/api/oauth2/authorize?client_id=" + Program._client.CurrentUser.Id + "&permissions=388161&scope=bot>\n" +
                    "請先通知 " + Program.ApplicatonOwner.Mention + " 取得邀請同意後再行邀請\n" +
                    "另外，若伺服器人數低於10人或發現未有發車情況，將會退出伺服器");
            }
            catch (Exception) { await ReplyAsync("無法私訊，請確認已開啟伺服器內成員私訊許可"); }
        }

        [Command("Status")]
        [Summary("顯示機器人目前的狀態")]
        [Alias("Stats")]
        public async Task StatusAsync()
        {
            int temp = 0;
            foreach (SocketGuild item in Program._client.Guilds) temp += item.MemberCount;

            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.Title = "飆車小幫手 " + Program.VERSION;
#if DEBUG
            embedBuilder.Title += " (測試版)";
#endif

            embedBuilder.AddField("作者", "孤之界#1121", true);
            embedBuilder.AddField("擁有者", Program.ApplicatonOwner.Username + "#" + Program.ApplicatonOwner.Discriminator, true);
            embedBuilder.AddField("看過的本子數量", Program.ListBookLogData.Count.ToString(), true);
            embedBuilder.AddField("狀態", $"伺服器 {Program._client.Guilds.Count}\n服務成員數 {temp}", false);
            embedBuilder.AddField("上線時間", $"{Program.stopWatch.Elapsed.Days} 天 {Program.stopWatch.Elapsed.Hours} 小時 {Program.stopWatch.Elapsed.Minutes} 分鐘 {Program.stopWatch.Elapsed.Seconds} 秒", false);

            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("GetExToken")]
        [Summary("輸入E-Hentai的ID，丟出完整網址\n若頻道為NSFW的話則會直接丟出詳細資料\n例: !!GetExToken 1451369\n回傳: https://exhentai.org/g/1451369/e25f951bb3/")]
        [Alias("GET")]
        public async Task GetExTokenAsync(uint id = 0)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    string result;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        result = webClient.DownloadString($"https://api.junrasp.nctu.me/?id={id}");
                    else
                        result = webClient.DownloadString($"http://127.0.0.1:81/?id={id}");

                    if (result.StartsWith("{}"))
                        await ReplyAsync($"ID {id} 無資料");
                    else
                    {
                        API.GalleryData galleryData = JsonConvert.DeserializeObject<API.GalleryData>(result);
                        if (((ITextChannel)Context.Channel).IsNsfw || galleryData.Type == "Non-H") EHentai.GetData($"exhentai.org/g/{galleryData.ID}/{galleryData.Token}", Context);
                        else await ReplyAsync($"https://exhentai.org/g/{galleryData.ID}/{galleryData.Token}");
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync("因系統問題，暫不開放使用");
                Log.FormatColorWrite(ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.DarkRed);
            }
        }

        [Command("Sauce")]
        [Summary("以圖搜圖，參數可指定向上略過幾張圖片來做搜尋\n也可以直接丟出網址來搜尋")]
        [Priority(1)]
        public async Task SauceAsync([Summary("向上略過幾張圖片")]int skip = 0)
        {
            var message = (await Context.Channel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false)).Where((x) =>
            x.Attachments.Count > 0 || 
            (x.Content.StartsWith("https://") && AllowedFileTypes.Any((x2) => x2 ==  System.IO.Path.GetExtension(x.Content))))
                .Skip(skip).Take(1).FirstOrDefault();
            if (message == null) { await ReplyAsync("不存在可搜尋的圖片"); return; }

            await SauceAsync(message.Attachments.Count > 0 ? message.Attachments.First().Url : message.Content);
        }

        [Command("Sauce")]
        [Priority(0)]
        public async Task SauceAsync([Summary("網址")]string url)
        {
            try
            {
                var result = await wrapper.GetSauceAsync(url).ConfigureAwait(false);
                if (result != null)
                {
                    List<string> description = new List<string>();
                    foreach (var item in result)
                    {
                        if (item.Index == SauceNAO.SiteIndex.nHentai) description.Add($"NHentai {item.Similarity}% 相似度");
                        else if (item.Sources != null) description.Add($"[{item.DB}]({item.Sources[0]}) {item.Similarity}% 相似度");
                    }

                    EmbedBuilder embedBuilder = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(result[0].Title)
                        .WithDescription(string.Join('\n', description));

                    if ((result[0].Rating == SauceNAO.SourceRating.Nsfw && ((ITextChannel)Context.Channel).IsNsfw) || result[0].Rating == SauceNAO.SourceRating.Safe)
                        embedBuilder.WithThumbnailUrl(result[0].Thumbnail);

                    await ReplyAsync(null, false, embedBuilder.Build());
                }
                else
                {
                    await ReplyAsync($"搜尋失敗");
                }
            }
            catch (Exception ex) { await ReplyAsync("搜尋失敗"); Log.FormatColorWrite(ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.DarkRed); }
        }
    }
}
