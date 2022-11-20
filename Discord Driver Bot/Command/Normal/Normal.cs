using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Driver_Bot.HttpClients.Ascii2D;
using Discord_Driver_Bot.HttpClients.SauceNAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Command.Normal
{
    public class Normal : TopLevelModule
    {
        private DiscordSocketClient _client;
        private SauceNAOClient _sauceNAOClient;
        private Ascii2DClient _ascii2DClient;
        private IHttpClientFactory _httpClientFactory;
        private string[] AllowedFileTypes { get; } = new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".svg", ".webp" };

        public Normal(DiscordSocketClient client,
            SauceNAOClient sauceNAOClient, Ascii2DClient ascii2DClient, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _sauceNAOClient = sauceNAOClient;
            _ascii2DClient = ascii2DClient;
            _httpClientFactory = httpClientFactory;
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
                        await Context.Channel.SendErrorAsync($"我不知道 {item} 所代表的網站，請使用 `!!godsay 本子網址縮寫 神的語言`");
                        return;
                }
                
                if (host != "")
                {
                    await GodSayAsync(host, item);
                }
            }
        }

        [Command("GodSay")]
        [Summary("顯示神所說的文字\n縮寫為p (普通頻道可用)\nw, e-, ex, h (限NSFW頻道使用)\n例:\n!!GodSay w 40600\n!!GodSay ex 1496326/aa30f4bfae")]
        [Alias("GS")]
        public async Task GodSayAsync([Summary("網站")] string host = null, [Summary("神的語言")] string godSay = null)
        {
            if (host == null) { await ReplyAsync(Context.User.Mention + " 網站錯誤"); return; }
            if (godSay == null) { await ReplyAsync(Context.User.Mention + " 沒有神的語言"); return; }

            string url;

            switch (host.ToLower())
            {
                //case "n":
                //    url = string.Format("https://nhentai.net/g/{0}", godSay);
                //    break;
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
                    await Context.Channel.SendErrorAsync($"找不到 {host} 的縮寫網站");
                    return;
            }

            try
            {
                if (url != "" && await Gallery.Function.ShowGalleryInfoAsync(url, Context.Guild, Context.Channel, Context.User))
                {
                    SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                }

            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.Message);
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
        [Summary("查本本，可搜尋的網站有ex" +
            "\n預設搜尋ExHentai" +
            "\n如關鍵字有空白，請在關鍵字錢後加上\"\"" +
            "\n\n例:" +
            "\n!!Search ex \"空色れん\" 1")]        
        [Alias("S")]
        [RequireNsfw]
        public async Task SearchAsync([Summary("搜尋網站")] string host = "ex", [Summary("本子關鍵字")]string keyWord = null, [Summary("頁數")]int page = 1)
        {
            if (keyWord == null) { await ReplyAsync("缺少本子關鍵字，你以為我會通靈嗎"); return; }

            switch (host)
            {
                case "ex":
                case "e-":
                    {
                        var result = await Gallery.SearchMulti.SearchExHentai(keyWord, page--);
                        if (result == null) await Context.Channel.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果");

                        await Context.SendPaginatedConfirmAsync(0, (row) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithUrl(result.SearchURL)
                            .WithTitle($"ExHentai 搜尋 `{keyWord}` 的結果如下")
                            .WithDescription($"共 {result.SearchCount} 本，合計 {(result.SearchCount / 25) + 1} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`/s ex \"{keyWord}\" 頁數`");
                            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                            return embedBuilder;
                        }, result.BookData.Count, 7);
                        break;
                    }
                //case "n":
                //    {
                //        var result = await Gallery.SearchMulti.SearchNHentaiAsync(keyWord, page);
                //        if (result == null) await Context.Channel.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果");

                //        await Context.SendPaginatedConfirmAsync(0, (row) =>
                //        {
                //            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                //            .WithUrl(result.SearchURL)
                //            .WithTitle($"NHentai 搜尋 `{keyWord}` 的結果如下")
                //            .WithDescription($"共 {result.SearchCount} 本，合計 {(result.SearchCount / 25) + 1} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`!!s n \"{keyWord}\" 頁數`");
                //            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                //            return embedBuilder;
                //        }, result.BookData.Count, 7);
                //        break;
                //    }
                default:
                    await Context.Channel.SendErrorAsync($"我不知道 {host} 是甚麼網站");
                    break;
            }
        }

        [Command("Invite")]
        [Summary("取得邀請連結")]
        public async Task InviteAsync()
        {
            try
            {
                await (await Context.Message.Author.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder().WithOkColor()
                    .WithDescription("<https://discordapp.com/api/oauth2/authorize?client_id=" + Program._client.CurrentUser.Id + "&permissions=388161&scope=bot%20applications.commands>").Build());
            }
            catch (Exception) { await Context.Channel.SendErrorAsync("無法私訊，請確認已開啟伺服器內成員私訊許可"); }
        }

        [Command("Status")]
        [Summary("顯示機器人目前的狀態")]
        [Alias("Stats")]
        public async Task StatusAsync()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.WithTitle("飆車小幫手");
#if DEBUG
            embedBuilder.Title += " (測試版)";
#endif

            embedBuilder.WithDescription($"建置版本 {Program.VERSION}");
            embedBuilder.AddField("作者", "孤之界#1121", true);
            embedBuilder.AddField("擁有者", $"{Program.ApplicatonOwner.Username}#{Program.ApplicatonOwner.Discriminator}", true);
            embedBuilder.AddField("狀態", $"伺服器 {_client.Guilds.Count}\n服務成員數 {_client.Guilds.Sum((x) => x.MemberCount)}", false);
            embedBuilder.AddField("看過的本子數量", Program.ListBookLogData.Count.ToString(), true);
            embedBuilder.AddField("上線時間", $"{Program.stopWatch.Elapsed:d\\天\\ hh\\:mm\\:ss}", false);

            await ReplyAsync(embed: embedBuilder.Build());
        }

        //[Command("GetExToken")]
        //[Summary("輸入E-Hentai的ID，丟出完整網址\n若頻道為NSFW的話則會直接丟出詳細資料\n例: !!GetExToken 1451369\n回傳: https://exhentai.org/g/1451369/e25f951bb3/")]
        //[Alias("GET")]
        //[RequireNsfw]
        //public async Task GetExTokenAsync(uint id = 0)
        //{
        //    try
        //    {
        //        using (WebClient webClient = new WebClient())
        //        {
        //            string result;
        //            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //                result = webClient.DownloadString($"https://?id={id}");
        //            else
        //                result = webClient.DownloadString($"http://127.0.0.1:81/?id={id}");

        //            if (result.StartsWith("{}"))
        //                await ReplyAsync($"ID {id} 無資料");
        //            else
        //            {
        //                API.GalleryData galleryData = JsonConvert.DeserializeObject<API.GalleryData>(result);
        //                if (((ITextChannel)Context.Channel).IsNsfw || galleryData.Type == "Non-H") EHentai.GetData($"exhentai.org/g/{galleryData.ID}/{galleryData.Token}", Context);
        //                else await ReplyAsync($"https://exhentai.org/g/{galleryData.ID}/{galleryData.Token}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await ReplyAsync("因系統問題，暫不開放使用");
        //        Log.FormatColorWrite(ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.DarkRed);
        //    }
        //}

        [Command("Sauce")]
        [Summary("以圖搜圖，參數可指定向上略過幾張圖片來做搜尋\n" +
            "也可以直接丟出網址來搜尋\n" +
            "或是透過對該圖片回應並輸入 `!!sauce` 同樣可以搜圖")]
        [Priority(1)]
        public async Task SauceAsync([Summary("向上略過幾張圖片")]int skip = 0)
        {
            IMessage message = Context.Message.ReferencedMessage;
            if (message == null)
                message = await GetLastAttachmentMessageAsync(skip);

            if (message == null ||
                message.Attachments.Count == 0 && !AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(message.Content)) ||
                message.Attachments.Count >= 1 && !AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(message.Attachments.First().Url)))
            { 
                await Context.Channel.SendErrorAsync("不存在可搜尋的圖片"); 
                return;
            }

            await SauceAsync(message.Attachments.Count > 0 ? message.Attachments.First().Url : message.Content);
        }

        [Command("Sauce")]
        [Priority(0)]
        public async Task SauceAsync([Summary("網址")]string url)
        {
            try
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    var req = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    req.EnsureSuccessStatusCode();

                    if (req.Content.Headers.ContentLength > 5242880)
                    {
                        await Context.Channel.SendErrorAsync("Ascii2D搜尋失敗，圖檔不可大於5MB");
                    }
                    else
                    {
                        var ascii2dResult = _ascii2DClient.FindAsync(url).Take(3);
                        if (ascii2dResult != null)
                        {
                            try
                            {
                                List<string> description = new List<string>();
                                await foreach (var item in ascii2dResult)
                                {
                                    if (item.Host == "dlsite")
                                        description.Add($"{Format.Url(item.Host, item.URL)} {item.Title}");
                                    else
                                        description.Add($"{Format.Url(item.Host, item.URL)} {item.Title} ({item.Author})");
                                }

                                EmbedBuilder embedBuilder = new EmbedBuilder()
                                    .WithOkColor()
                                    .WithTitle(ascii2dResult.FirstAsync().Result.Title)
                                    .WithDescription(string.Join('\n', description))
                                    .WithFooter("Ascii2D");
                                embedBuilder.WithThumbnailUrl(ascii2dResult.FirstAsync().Result.Thumbnail);

                                await ReplyAsync(null, false, embedBuilder.Build());
                            }
                            catch (Exception ex)
                            {
                                await Context.Channel.SendErrorAsync("Ascii2D搜尋失敗，未知的錯誤");
                                Log.Error(ex.ToString());
                            }
                        }
                        else
                        {
                            await Context.Channel.SendErrorAsync("Ascii2D搜尋失敗，未知的錯誤");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync("Ascii2D搜尋失敗，未知的錯誤");
                    Log.Error(url);
                    Log.Error(ex.ToString());
                }

                var sauceResult = await _sauceNAOClient.GetSauceAsync(url).ConfigureAwait(false);
                if (sauceResult != null)
                {
                    List<string> description = new List<string>();
                    foreach (var item in sauceResult)
                    {
                        if (item.Index == SauceNAOClient.SiteIndex.nHentai) description.Add($"NHentai {item.Similarity}% 相似度");
                        else if (item.Sources != null) description.Add($"[{item.DB}]({item.Sources}) {item.Similarity}% 相似度");
                    }

                    EmbedBuilder embedBuilder = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(sauceResult[0].Title)
                        .WithDescription(string.Join('\n', description))
                        .WithFooter("SauceNAO");

                    if ((sauceResult[0].Rating == SauceNAOClient.SourceRating.Nsfw && ((ITextChannel)Context.Channel).IsNsfw) || sauceResult[0].Rating == SauceNAOClient.SourceRating.Safe)
                        embedBuilder.WithThumbnailUrl(sauceResult[0].Thumbnail);

                    await ReplyAsync(null, false, embedBuilder.Build());
                }
                else
                {
                    await Context.Channel.SendErrorAsync($"SauceNAO搜尋失敗");
                }
            }
            catch (Exception ex) 
            {
                await Context.Channel.SendErrorAsync("搜尋失敗");
                Log.Error(ex.ToString()); 
            }
        }

        private async Task<IMessage> GetLastAttachmentMessageAsync(int skip = 0)
        {
            return (await Context.Channel.GetMessagesAsync().FlattenAsync().ConfigureAwait(false)).Where((x) =>
          x.Attachments.Count > 0 && AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(x.Attachments.First().Url)) ||
          (x.Content.StartsWith("https://") && AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(x.Content))))
              .Skip(skip).Take(1).FirstOrDefault();
        }
    } 
}
