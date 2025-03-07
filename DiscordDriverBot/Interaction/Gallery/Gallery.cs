using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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
            //NHentai
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
                case 9:
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

            try
            {
                if (url != "" && await DiscordDriverBot.Gallery.Function.ShowGalleryInfoAsync(url, Context.Guild, Context.Channel, Context.User, Context))
                {
                    SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                }
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync(ex.Message, true, true);
            }
        }

        public enum Host
        {
            ExHentai,
            //NHentai,
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

            try
            {
                if (url != "" && await DiscordDriverBot.Gallery.Function.ShowGalleryInfoAsync(url, Context.Guild, Context.Channel, Context.User, Context))
                {
                    SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                }
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync(ex.Message, true);
            }
        }

        [SlashCommand("search", "查本本")]
        [CommandSummary("查本本，可搜尋的網站有ex\n" +
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
                        var result = await DiscordDriverBot.Gallery.SearchMulti.SearchExHentai(keyWord, page--);
                        if (result == null) { await Context.Interaction.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果", true, true); return; }
                        ;

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
                //case SearchHost.NHentai:
                //    {
                //        var result = await DiscordDriverBot.Gallery.SearchMulti.SearchNHentaiAsync(keyWord, page);
                //        if (result == null) { await Context.Interaction.SendErrorAsync("搜尋失敗，可能是該關鍵字無搜尋結果", true, true); return; };

                //        await Context.SendPaginatedConfirmAsync(0, (row) =>
                //        {
                //            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                //            .WithUrl(result.SearchURL)
                //            .WithTitle($"NHentai 搜尋 `{keyWord}` 的結果如下")
                //            .WithDescription($"共 {result.SearchCount} 本，合計 {(result.SearchCount / 25) + 1} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n`/gallery search NHentai \"{keyWord}\" 頁數`");
                //            result.BookData.Skip(row * 7).Take(7).ToList().ForEach((x) => embedBuilder.AddField(x.Title, Format.Url(x.Language, x.URL), false));

                //            return embedBuilder;
                //        }, result.BookData.Count, 7, isFollowup: true);
                //        break;
                //    }
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
                var result = await _service.SauceFromAscii2DAsync(url);
                if (string.IsNullOrEmpty(result.ErrorMessage) && result.Embed != null)
                    await FollowupAsync(embed: result.Embed, ephemeral: ephemeral);
                else
                    await Context.Interaction.SendErrorAsync(result.ErrorMessage, true);

                result = await _service.SauceFromSauceNAOAsync(url);
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

        [MessageCommand("搜尋 Ascii2D")]
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

            try
            {
                bool hasAnyResult = false;
                await DeferAsync();

                foreach (string item in content.Split(new char[] { '\n' }))
                {
                    if (await DiscordDriverBot.Gallery.Function.ShowGalleryInfoAsync(item, Context.Guild, message.Channel, message.Author, Context))
                    {
                        hasAnyResult = true;
                        Log.FormatColorWrite($"[{Context.Guild.Name}/{Context.Channel.Name}]{Context.User.Username}: {item}", ConsoleColor.Gray);
                        SQLite.SQLiteFunction.UpdateGuildReadedBook(Context.Guild.Id);
                    }
                }

                if (!hasAnyResult)
                    await Context.Interaction.SendErrorAsync("無任何可供解析的網址", true);
            }
            catch (Exception ex)
            {
                await Context.Interaction.SendErrorAsync("解析失敗，未知的錯誤", true);
                Log.Error(ex.Demystify(), content);
            }
        }
    }
}
