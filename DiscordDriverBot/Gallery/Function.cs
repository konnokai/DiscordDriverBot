using Discord;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Gallery
{
    public static class Function
    {
        public enum BookHost
        {
            Wnacg, NHentai, E_Hentai, ExHentai, Hitomi, Pixiv, None
        }

        public static BookHost CheckBookHost(string url)
        {
            url = FilterUrl(url).Replace("https://", "").Replace("http://", "").Replace("www.", "").Replace("m.", "");
            if (url.StartsWith("wnacg")) return BookHost.Wnacg;
            if (url.StartsWith("nhentai.net/g/")) return BookHost.NHentai;
            if (url.StartsWith("e-hentai.org/g/") || url.StartsWith("e-hentai.org/s/")) return BookHost.E_Hentai;
            if (url.StartsWith("exhentai.org/g/") || url.StartsWith("exhentai.org/s/")) return BookHost.ExHentai;
            if (url.StartsWith("hitomi.la/galleries/") || url.StartsWith("hitomi.la/reader/")) return BookHost.Hitomi;
            if (url.StartsWith("pixiv.net")) return BookHost.Pixiv;
            return BookHost.None;
        }

        public static string FilterUrl(string url)
        {
            try
            {
                string tempUrl = "";
                foreach (char item in url)
                {
                    if (item == 38 || (item >= 45 && item <= 58) || item == 61 || item == 63 || (item >= 65 && item <= 90) || item == 95 || (item >= 97 && item <= 122))
                        tempUrl += item;
                }
                url = tempUrl.Substring(tempUrl.IndexOf("http"));
            }
            catch (Exception) { }
            return url;
        }

        public static async Task<bool> ShowGalleryInfoAsync(string url, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext = null)
        {
            url = FilterUrl(url).Replace("https://", "").Replace("http://", "").Replace("www.", "").Replace("m.", "");
            bool IsNSFW = (messageChannel as ITextChannel).IsNsfw;

            switch (CheckBookHost(url))
            {
                case BookHost.Wnacg:
                    {
                        if (!IsNSFW && user.Id != Program.ApplicatonOwner.Id) return false;
                        await Host.Wnacg.GetDataAsync(url, guild, messageChannel, user, interactionContext);
                        return true;
                    }
                case BookHost.E_Hentai:
                case BookHost.ExHentai:
                    {
                        if (!IsNSFW && user.Id != Program.ApplicatonOwner.Id) return false;
                        await Host.EHentai.EHentai.GetDataAsync(url, guild, messageChannel, user, interactionContext);
                        return true;
                    }
                //case BookHost.NHentai:
                //    {
                //        if (!IsNSFW && user.Id != Program.ApplicatonOwner.Id) return false;
                //        await Host.NHentai.GetDataAsync(url, guild, messageChannel, user, interactionContext);
                //        return true;
                //    }
                case BookHost.Hitomi:
                    {
                        if (!IsNSFW && user.Id != Program.ApplicatonOwner.Id) return false;
                        await Host.Hitomi.GetDataAsync(url, guild, messageChannel, user, interactionContext);
                        return true;
                    }
                case BookHost.Pixiv:
                    {
                        if ((url.Contains("member_illust.php") && url.Contains("illust_id")) || url.Contains("artworks") /*|| url.Contains("users")*/)
                        {
                            await Host.Pixiv.Pixiv.GetDataAsync(url, guild, messageChannel, user, interactionContext);
                            return true;
                        }
                        return false;
                    }
                default:
                    {
                        return false;
                    }
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
                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpWebRequest.GetResponse().GetResponseStream();
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
