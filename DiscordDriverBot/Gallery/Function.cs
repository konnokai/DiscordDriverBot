using Discord;
using Discord.Net;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Gallery
{
    public static class Function
    {
        public static async Task<GalleryShowResult> ShowGalleryInfoAsync(GalleryLink link, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext = null)
        {
            if (link == null) return GalleryShowResult.Failed;

            if (guild == null || messageChannel is not ITextChannel textChannel) return GalleryShowResult.Blocked;
            if (link.Host != BookHost.Pixiv && !textChannel.IsNsfw && user.Id != Program.ApplicatonOwner.Id) return GalleryShowResult.Blocked;

            bool shown = link.Host switch
            {
                BookHost.Wnacg => await Host.Wnacg.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.E_Hentai or BookHost.ExHentai => await Host.EHentai.EHentai.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.Hitomi => await Host.Hitomi.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.NHentai => await Host.NHentai.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.Pixiv => link.Kind == GalleryLinkKind.PixivUser
                    ? await Host.Pixiv.Pixiv.GetUserDataAsync(link, guild, messageChannel, user, interactionContext)
                    : await Host.Pixiv.Pixiv.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                _ => false
            };

            return shown ? GalleryShowResult.Shown : GalleryShowResult.Failed;
        }

        /// <summary>
        /// 解析完把原訊息的連結預覽收掉，不然同一張圖會在頻道裡出現兩次
        /// </summary>
        public static async Task SuppressEmbedsAsync(this IUserMessage message)
        {
            if (message == null || message.Author.IsBot || message.IsSuppressed) return;

            // Discord 的連結預覽是非同步抓的，太早送 suppress 會被後到的預覽蓋掉，先等它出現
            // shortcut: 只等 cache 更新，訊息被洗出 50 筆 cache 就補不到，需要時改抓 REST
            for (int i = 0; i < 5 && !message.IsSuppressed && message.Embeds.Count == 0; i++)
                await Task.Delay(1000);

            if (message.IsSuppressed || message.Embeds.Count == 0) return;

            try
            {
                await message.ModifyAsync(x => x.Flags = (message.Flags ?? MessageFlags.None) | MessageFlags.SuppressEmbeds);
            }
            catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions or DiscordErrorCode.UnknownMessage)
            {
                // 沒權限或訊息已刪除時預覽留著就好，不影響解析結果
                Log.Info($"略過移除訊息 {message.Id} 的預覽: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"移除訊息 {message.Id} 的預覽失敗");
            }
        }

        public static bool GetIDIsExist(string url)
        {
            if (url.ToLower().Contains("wnacg"))
            {
                HtmlAgilityPack.HtmlWeb htmlWeb = new HtmlAgilityPack.HtmlWeb();
                var list = htmlWeb.Load(url).DocumentNode.Descendants();
                return !list.Any((x) => x.InnerText.StartsWith("沒有訪問權限") || x.InnerText.StartsWith("您要訪問的相冊不存在"));
            }
            else
            {
                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Send(new HttpRequestMessage(HttpMethod.Head, url)).EnsureSuccessStatusCode();
                    return true;
                }
                catch (Exception) { }
            }

            return false;
        }

        public static string FormatBookName(this string text)
        {
            try
            {
                foreach (Match m in Regex.Matches(text, @"(\[.*?\])|(\(.*?\))"))
                    text = text.Replace(m.Value, string.Empty);
            }
            catch (Exception) { }

            return text.Trim();
        }
    }
}
