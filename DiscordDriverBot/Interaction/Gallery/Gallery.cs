using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordDriverBot.Gallery;
using DiscordDriverBot.Interaction.Attribute;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordDriverBot.Interaction.Gallery
{
    [CommandContextType(InteractionContextType.Guild)]
    [Group("gallery", "本本用")]
    public class Gallery : TopLevelModule<GalleryService>
    {
        public enum SearchHost
        {
            ExHentai,
            NHentai
        }

        [SlashCommand("auto-god-say", "依據輸入的字數來判斷網站並顯示神所說的句子")]
        [CommandSummary("依據輸入的字數來判斷網站並顯示神所說的句子\n\n" +
           "字數自動判斷的網站如下\n" +
           "5-6 為 Wnacg，7 為 Hitomi\n" +
           "8-9 為 Pixiv，gid/token 為 ExHentai")]
        [CommandExample("52600", "95741451", "1496326/aa30f4bfae")]
        public async Task AutoGodSayAsync([Summary("神的語言")] string godSay)
        {
            await Context.Interaction.DeferAsync();

            BookHost host = GalleryUrl.GuessGodSayHost(godSay);
            string url = GalleryUrl.GodSayUrl(host, godSay);
            if (url == null || !GalleryUrl.TryParse(url, out GalleryLink link))
            {
                await Context.Interaction.SendErrorAsync($"{Context.User.Mention} 我不知道 {godSay} 所代表的網站，請使用 `/gallery god-say 本子網址 神的語言`", true, true);
                return;
            }

            try
            {
                GalleryShowResult result = await DiscordDriverBot.Gallery.Function.ShowGalleryInfoAsync(link, Context.Guild, Context.Channel, Context.User, Context);
                if (result == GalleryShowResult.Shown)
                    SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                else if (result == GalleryShowResult.Blocked)
                    await Context.Interaction.SendErrorAsync($"{Context.User.Mention} 此網址需要在 NSFW 頻道使用", true, true);
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync(ex.Message, true, true);
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
            "Wancg, ExHentai, Hitomi, NHentai (限 NSFW 頻道使用)\n")]
        [CommandExample("w 56600", "ex 1496326/aa30f4bfae")]
        public async Task GodSayAsync([Summary("網站")] Host host, [Summary("神的語言")] string godSay)
        {
            await Context.Interaction.DeferAsync();

            BookHost bookHost = host switch
            {
                Host.ExHentai => BookHost.ExHentai,
                Host.NHentai => BookHost.NHentai,
                Host.Wancg => BookHost.Wnacg,
                Host.Hitomi => BookHost.Hitomi,
                Host.Pixiv => BookHost.Pixiv,
                _ => BookHost.None
            };

            string url = GalleryUrl.GodSayUrl(bookHost, godSay);
            if (url == null || !GalleryUrl.TryParse(url, out GalleryLink link))
            {
                await Context.Interaction.SendErrorAsync($"{Context.User.Mention} 網址格式錯誤，請確認 ID 或加上 token", true, true);
                return;
            }

            try
            {
                GalleryShowResult result = await DiscordDriverBot.Gallery.Function.ShowGalleryInfoAsync(link, Context.Guild, Context.Channel, Context.User, Context);
                if (result == GalleryShowResult.Shown)
                    SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                else if (result == GalleryShowResult.Blocked)
                    await Context.Interaction.SendErrorAsync($"{Context.User.Mention} 此網址需要在 NSFW 頻道使用", true, true);
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync(ex.Message, true, true);
            }
        }

        [SlashCommand("search", "查本本")]
        [CommandSummary("查本本，可搜尋的網站有 ExHentai、NHentai\n" +
            "預設搜尋 ExHentai")]
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
                        var result = await DiscordDriverBot.Gallery.SearchMulti.SearchExHentai(keyWord, page);
                        if (result == null) { await Context.Interaction.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果", true, true); return; }
                        ;

                        await Context.SendPaginatedConfirmAsync(0, (row) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithUrl(result.SearchURL)
                            .WithTitle($"ExHentai 搜尋 `{keyWord}` 的結果如下")
                            .WithDescription($"共 {result.SearchCount} 本，合計 {result.TotalPages} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`/gallery search ExHentai \"{keyWord}\" 頁數`");
                            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                            return embedBuilder;
                        }, result.BookData.Count, 7, isFollowup: true);
                        break;
                    }
                case SearchHost.NHentai:
                    {
                        var result = await DiscordDriverBot.Gallery.SearchMulti.SearchNHentaiAsync(keyWord, page);
                        if (result == null) { await Context.Interaction.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果", true, true); return; };

                        await Context.SendPaginatedConfirmAsync(0, (row) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithUrl(result.SearchURL)
                            .WithTitle($"NHentai 搜尋 `{keyWord}` 的結果如下")
                            .WithDescription($"共 {result.SearchCount} 本，合計 {result.TotalPages} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`/gallery search NHentai \"{keyWord}\" 頁數`");
                            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                            return embedBuilder;
                        }, result.BookData.Count, 7, isFollowup: true);
                        break;
                    }
                default:
                    break;
            }
        }

        [SlashCommand("sauce-from-attachment", "以附件來搜圖")]
        public async Task SauceFromAttachmentAsync([Summary("圖片附件")] Attachment attachment)
            => await SauceFromUrlAsync(attachment.Url);

        [SlashCommand("sauce-from-url", "以圖片網址來搜圖")]
        public async Task SauceFromUrlAsync([Summary("圖片網址")] string url)
        {
            if (!_service.AllowedFileTypes.Any((x) => x == Path.GetExtension(url)))
            {
                await Context.Interaction.SendErrorAsync($"副檔名: \"{Path.GetExtension(url)}\" 不可用於搜尋");
                return;
            }

            bool ephemeral = !(Context.Channel is SocketTextChannel && (Context.Channel as SocketTextChannel).IsNsfw);
            await DeferAsync(ephemeral);

            try
            {
                // 暫時關閉 Ascii2D 搜尋
                //var result = await _service.SauceFromAscii2DAsync(url);
                //if (string.IsNullOrEmpty(result.ErrorMessage) && result.Embed != null)
                //    await FollowupAsync(embed: result.Embed, ephemeral: ephemeral);
                //else
                //    await Context.Interaction.SendErrorAsync(result.ErrorMessage, true);

                var result = await _service.SauceFromSauceNAOAsync(url);
                if (string.IsNullOrEmpty(result.ErrorMessage) && result.Embed != null)
                    await FollowupAsync(embed: result.Embed, ephemeral: ephemeral);
                else
                    await Context.Interaction.SendErrorAsync(result.ErrorMessage, true);
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync("搜尋失敗", true);
                Log.Error(ex.Demystify().ToString());
            }
        }

        //[MessageCommand("搜尋 Ascii2D")]
        public async Task SauceAscii2DAsync(IMessage message)
        {
            if (message == null ||
               message.Attachments.Count == 0 && !_service.AllowedFileTypes.Any((x2) => x2 == Path.GetExtension(message.Content.Split('?')[0])) ||
               message.Attachments.Count >= 1 && !_service.AllowedFileTypes.Any((x2) => x2 == Path.GetExtension(message.Attachments.First().Url.Split('?')[0])))
            {
                await Context.Interaction.SendErrorAsync("不存在可搜尋的圖片");
                return;
            }

            string url = message.Attachments.Count > 0 ? message.Attachments.First().Url : message.Content;
            await DeferAsync(!(Context.Channel is SocketTextChannel && (Context.Channel as SocketTextChannel).IsNsfw));

            var result = await _service.SauceFromAscii2DAsync(url);
            if (string.IsNullOrEmpty(result.ErrorMessage) && result.Embed != null)
                await FollowupAsync(text: Format.Url("搜尋的圖片", message.GetJumpUrl()), embed: result.Embed);
            else
                await Context.Interaction.SendErrorAsync(result.ErrorMessage, true);
        }

        [MessageCommand("搜尋 SauceNAO")]
        public async Task SauceSauceNAOAsync(IMessage message)
        {
            if (message == null ||
               message.Attachments.Count == 0 && !_service.AllowedFileTypes.Any((x2) => x2 == Path.GetExtension(message.Content.Split('?')[0])) ||
               message.Attachments.Count >= 1 && !_service.AllowedFileTypes.Any((x2) => x2 == Path.GetExtension(message.Attachments.First().Url.Split('?')[0])))
            {
                await Context.Interaction.SendErrorAsync("不存在可搜尋的圖片");
                return;
            }

            string url = message.Attachments.Count > 0 ? message.Attachments.First().Url : message.Content;
            await DeferAsync(!(Context.Channel is SocketTextChannel && (Context.Channel as SocketTextChannel).IsNsfw));

            var result = await _service.SauceFromSauceNAOAsync(url);
            if (string.IsNullOrEmpty(result.ErrorMessage) && result.Embed != null)
                await FollowupAsync(text: Format.Url("搜尋的圖片", message.GetJumpUrl()), embed: result.Embed);
            else
                await Context.Interaction.SendErrorAsync(result.ErrorMessage, true);
        }

        [MessageCommand("解析訊息內的網址")]
        public async Task ParseGalleryUrlAsync(IMessage message)
        {
            string content = message.Content;
            if (string.IsNullOrEmpty(content))
            {
                await Context.Interaction.SendErrorAsync("不存在可解析的文字");
                return;
            }

            var links = GalleryUrl.Extract(content).Take(5).ToList();
            if (links.Count == 0)
            {
                await Context.Interaction.SendErrorAsync("無任何可供解析的網址");
                return;
            }

            try
            {
                bool hasAnyResult = false;
                bool isBlocked = false;
                bool hasFailed = false;
                await DeferAsync();

                foreach (var link in links)
                {
                    GalleryShowResult result = await DiscordDriverBot.Gallery.Function.ShowGalleryInfoAsync(link, Context.Guild, Context.Channel, Context.User, Context);
                    if (result == GalleryShowResult.Shown)
                    {
                        hasAnyResult = true;
                        Log.FormatColorWrite($"[{Context.Guild.Name}/{Context.Channel.Name}]{Context.User.Username}: {link.SourceUrl}", ConsoleColor.Gray);
                        if (link.IsBook) SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                    }
                    else if (result == GalleryShowResult.Blocked)
                    {
                        isBlocked = true;
                    }
                    else
                    {
                        hasFailed = true;
                    }
                }

                if (isBlocked && (hasAnyResult || hasFailed))
                    await Context.Interaction.SendErrorAsync("部分網址需要在 NSFW 頻道使用", true, true);
                else if (!hasAnyResult && !hasFailed)
                    await Context.Interaction.SendErrorAsync(isBlocked ? "此網址需要在 NSFW 頻道使用" : "無任何可供解析的網址", true, true);
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync("解析失敗，未知的錯誤", true, true);
                Log.Error(ex.Demystify(), content);
            }
        }
    }
}
