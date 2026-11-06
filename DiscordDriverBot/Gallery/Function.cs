using Discord;
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

            if (link.Host == BookHost.NHentai)
            {
                if (interactionContext != null)
                    await interactionContext.Interaction.FollowupAsync("NHentai 目前停用", ephemeral: true);
                return GalleryShowResult.Failed;
            }

            if (guild == null || messageChannel is not ITextChannel textChannel) return GalleryShowResult.Blocked;
            if (link.Host != BookHost.Pixiv && !textChannel.IsNsfw && user.Id != Program.ApplicatonOwner.Id) return GalleryShowResult.Blocked;

            bool shown = link.Host switch
            {
                BookHost.Wnacg => await Host.Wnacg.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.E_Hentai or BookHost.ExHentai => await Host.EHentai.EHentai.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.Hitomi => await Host.Hitomi.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                BookHost.Pixiv => link.Kind == GalleryLinkKind.PixivUser
                    ? await Host.Pixiv.Pixiv.GetUserDataAsync(link, guild, messageChannel, user, interactionContext)
                    : await Host.Pixiv.Pixiv.GetDataAsync(link, guild, messageChannel, user, interactionContext),
                // NHentai 目前停用
                _ => false
            };

            return shown ? GalleryShowResult.Shown : GalleryShowResult.Failed;
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
