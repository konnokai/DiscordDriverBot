using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discord_Driver_Bot.Interaction.Attribute;
using Discord_Driver_Bot.HttpClients.Ascii2D;
using Discord_Driver_Bot.HttpClients.SauceNAO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Net.Http;

namespace Discord_Driver_Bot.Interaction.Gallery
{
    [Group("gallery", "本本用")]
    public class Gallery : TopLevelModule
    {
        private Ascii2DClient _ascii2DClient;
        private SauceNAOClient _sauceNAOClient;
        private IHttpClientFactory _httpClientFactory;
        private string[] AllowedFileTypes { get; } = new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".svg", ".webp" };

        public Gallery(Ascii2DClient ascii2DClient, SauceNAOClient sauceNAOClient, IHttpClientFactory httpClientFactory)
        {
            _ascii2DClient = ascii2DClient;
            _sauceNAOClient = sauceNAOClient;
            _httpClientFactory = httpClientFactory;
        }

        public enum SearchHost
        {
            ExHentai,
            NHentai
        }

        [SlashCommand("auto-god-say", "依據輸入的字數來判斷網站並顯示神所說的句子")]
        [CommandSummary("依據輸入的字數來判斷網站並顯示神所說的句子\n\n" +
           "字數自動判斷的網站如下\n" +
           "5為Wnacg，7為Hitomi\n" +
           "8為Pixiv，18跟19為ExHentai")]
        [CommandExample("52600", "95741451", "1496326/aa30f4bfae")]
        public async Task AutoGodSayAsync([Summary("神的語言")] string godSay)
        {
            await Context.Interaction.DeferAsync();
            string url;

            switch (godSay.Length)
            {
                case 5:
                    url = string.Format("https://www.wnacg.org/photos-index-aid-{0}.html", godSay);
                    break;
                //case 6:
                //    url = string.Format("https://nhentai.net/g/{0}", godSay);
                //    break;
                case 7:
                    url = string.Format("https://hitomi.la/galleries/{0}.html", godSay);
                    break;
                case 8:
                    url = string.Format("https://www.pixiv.net/artworks/{0}", godSay);
                    break;
                case 18:
                case 19:
                    url = string.Format("https://exhentai.org/g/{0}", godSay);
                    break;
                default:
                    await Context.Interaction.SendErrorAsync($"{Context.User.Mention} 我不知道 {godSay} 所代表的網站，請使用 `/utility god-say 本子網址 神的語言`", true, true);
                    return;
            }

            if (url != "" && await Discord_Driver_Bot.Gallery.Function.ShowGalleryInfoAsync(url, Context.Guild, Context.Channel, Context.User, Context))
            {
                SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
            }
        }

        public enum Host
        {
            ExHentai,
            NHentai,
            Wancg,
            Hitomi,
            Pixiv
        }

        [SlashCommand("god-say", "顯示神所說的文字")]
        [CommandSummary("顯示神所說的文字\n" +
            "Pixiv (普通頻道可用)\n" +
            "Wancg, ExHentai, Hitomi (限NSFW頻道使用)\n")]
        [CommandExample("w 56600", "ex 1496326/aa30f4bfae")]
        public async Task GodSayAsync([Summary("網站")] Host host, [Summary("神的語言")] string godSay)
        {
            await Context.Interaction.DeferAsync();
            string url = "";

            switch (host)
            {
                case Host.ExHentai:
                    url = string.Format("https://exhentai.org/g/{0}", godSay);
                    break;
                //case Host.NHentai:
                //    url = string.Format("https://nhentai.net/g/{0}", godSay);
                //    break;
                case Host.Wancg:
                    url = string.Format("https://www.wnacg.org/photos-index-aid-{0}.html", godSay);
                    break;
                case Host.Hitomi:
                    url = string.Format("https://hitomi.la/galleries/{0}.html", godSay);
                    break;
                case Host.Pixiv:
                    url = string.Format("https://www.pixiv.net/artworks/{0}", godSay);
                    break;
            }

            if (url != "" && await Discord_Driver_Bot.Gallery.Function.ShowGalleryInfoAsync(url, Context.Guild, Context.Channel, Context.User, Context))
            {
                SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
            }
        }

        [SlashCommand("search", "查本本")]
        [CommandSummary("查本本，可搜尋的網站有ex, n\n" +
            "預設搜尋ExHentai")]
        [CommandExample("ExHentai 空色れん")]
        [RequireNsfw]
        public async Task SearchAsync([Summary("搜尋網站")] SearchHost host, [Summary("本子關鍵字")] string keyWord, [Summary("頁數")] int page = 1)
        {
            if (keyWord == null) { await Context.Interaction.SendErrorAsync("缺少本子關鍵字，你以為我會通靈嗎"); return; }

            await Context.Interaction.DeferAsync();

            switch (host)
            {
                case SearchHost.ExHentai:
                    {
                        var result = await Discord_Driver_Bot.Gallery.SearchMulti.SearchExHentai(keyWord, page--);
                        if (result == null) { await Context.Interaction.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果", true, true); return; };

                        await Context.SendPaginatedConfirmAsync(0, (row) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithUrl(result.SearchURL)
                            .WithTitle($"ExHentai 搜尋 `{keyWord}` 的結果如下")
                            .WithDescription($"共 {result.SearchCount} 本，合計 {(result.SearchCount / 25) + 1} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`/gallery search ExHentai \"{keyWord}\" 頁數`");
                            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                            return embedBuilder;
                        }, result.BookData.Count, 7, isFollowup: true);
                        break;
                    }
                case SearchHost.NHentai:
                    {
                        var result = await Discord_Driver_Bot.Gallery.SearchMulti.SearchNHentaiAsync(keyWord, page);
                        if (result == null) { await Context.Interaction.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果", true, true); return; };

                        await Context.SendPaginatedConfirmAsync(0, (row) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithUrl(result.SearchURL)
                            .WithTitle($"NHentai 搜尋 `{keyWord}` 的結果如下")
                            .WithDescription($"共 {result.SearchCount} 本，合計 {(result.SearchCount / 25) + 1} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`/gallery search NHentai \"{keyWord}\" 頁數`");
                            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                            return embedBuilder;
                        }, result.BookData.Count, 7, isFollowup: true);
                        break;
                    }
                default:
                    break;
            }
        }

        [MessageCommand("搜尋Ascii2D")]
        public async Task SauceAscii2DAsync(IMessage message)
        {
            if (message == null ||
               message.Attachments.Count == 0 && !AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(message.Content)) ||
               message.Attachments.Count >= 1 && !AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(message.Attachments.First().Url)))
            {
                await Context.Interaction.SendErrorAsync("不存在可搜尋的圖片");
                return;
            }

            string url = message.Attachments.Count > 0 ? message.Attachments.First().Url : message.Content;
            await Context.Interaction.DeferAsync(!(Context.Channel is SocketTextChannel && (Context.Channel as SocketTextChannel).IsNsfw));

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var req = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                req.EnsureSuccessStatusCode();

                if (req.Content.Headers.ContentLength > 5242880)
                {
                    await Context.Interaction.SendErrorAsync("圖檔不可大於5MB", true);
                    return;
                }
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync("搜尋失敗，未知的錯誤", true);
                Log.Error(url);
                Log.Error(ex.ToString());
            }

            try
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
                            .WithThumbnailUrl(ascii2dResult.FirstAsync().Result.Thumbnail)
                            .WithFooter("Ascii2D");

                        await FollowupAsync(embed: embedBuilder.Build());
                    }
                    catch (Exception ex)
                    {
                        await Context.Interaction.SendErrorAsync("搜尋失敗，未知的錯誤", true);
                        Log.Error(url);
                        Log.Error(ex.ToString());
                    }
                }
                else
                {
                    await Context.Interaction.SendErrorAsync($"搜尋失敗，無回傳值", true);
                }
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync("搜尋失敗，未知的錯誤", true);
                Log.Error(url);
                Log.Error(ex.ToString());
            }
        }

        [MessageCommand("搜尋SauceNAO")]
        public async Task SauceSauceNAOAsync(IMessage message)
        {
            if (message == null ||
               message.Attachments.Count == 0 && !AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(message.Content)) ||
               message.Attachments.Count >= 1 && !AllowedFileTypes.Any((x2) => x2 == System.IO.Path.GetExtension(message.Attachments.First().Url)))
            {
                await Context.Interaction.SendErrorAsync("不存在可搜尋的圖片");
                return;
            }

            string url = message.Attachments.Count > 0 ? message.Attachments.First().Url : message.Content;
            await Context.Interaction.DeferAsync(!(Context.Channel is SocketTextChannel && (Context.Channel as SocketTextChannel).IsNsfw));

            try
            {
                var sauceResult = await _sauceNAOClient.GetSauceAsync(url).ConfigureAwait(false);
                if (sauceResult != null)
                {
                    List<string> description = new List<string>();
                    foreach (var item in sauceResult)
                    {
                        if (item.Index == SauceNAOClient.SiteIndex.nHentai) description.Add($"NHentai {item.Similarity}% 相似度");
                        else if (item.Sources != null) description.Add($"[{item.DB}]({item.Sources[0]}) {item.Similarity}% 相似度");
                    }

                    EmbedBuilder embedBuilder = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(sauceResult[0].Title)
                        .WithDescription(string.Join('\n', description))
                        .WithThumbnailUrl(sauceResult[0].Thumbnail)
                        .WithFooter("SauceNAO");

                    await FollowupAsync(embed: embedBuilder.Build());
                }
                else
                {
                    await Context.Interaction.SendErrorAsync("搜尋失敗，無回傳值", true);
                }
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync("搜尋失敗，未知的錯誤", true);
                Log.Error(url);
                Log.Error(ex.ToString());
            }
        }
    }
}
