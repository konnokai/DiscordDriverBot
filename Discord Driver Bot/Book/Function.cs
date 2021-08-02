using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Discord_Driver_Bot.Book
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
            if (url.StartsWith("e-hentai.org/g/")|| url.StartsWith("e-hentai.org/s/")) return BookHost.E_Hentai;
            if (url.StartsWith("exhentai.org/g/") || url.StartsWith("exhentai.org/s/")) return BookHost.ExHentai;
            if (url.StartsWith("hitomi.la/galleries/")) return BookHost.Hitomi;
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

        public static bool ShowBookInfo(string url, ICommandContext e)
        {
            url = FilterUrl(url).Replace("https://", "").Replace("http://", "").Replace("www.", "").Replace("m.", "");
            bool IsNSFW = (e.Channel as ITextChannel).IsNsfw;

            switch (CheckBookHost(url))
            {
                case BookHost.Wnacg:
                    {
                        if (!IsNSFW && e.Message.Author.Id != Program.ApplicatonOwner.Id) return false;
                        Host.Wnacg.GetData(url, e);
                        return true;
                    }
                case BookHost.E_Hentai:
                case BookHost.ExHentai:
                    {
                        if (!IsNSFW && e.Message.Author.Id != Program.ApplicatonOwner.Id) return false;
                        Host.EHentai.EHentai.GetData(url, e);
                        return true;
                    }
                case BookHost.NHentai:
                    {
                        if (!IsNSFW && e.Message.Author.Id != Program.ApplicatonOwner.Id) return false;
                        Host.NHentai.NHentai.GetData(url, e);
                        return true;
                    }
                case BookHost.Hitomi:
                    {
                        if (!IsNSFW && e.Message.Author.Id != Program.ApplicatonOwner.Id) return false;
                        Host.Hitomi.GetData(url, e);
                        return true;
                    }
                case BookHost.Pixiv:
                    {
                        if ((url.Contains("member_illust.php") && url.Contains("illust_id")) || url.Contains("artworks") /*|| url.Contains("users")*/)
                        {
                            Host.Pixiv.Pixiv.GetData(url, e);
                            return true;
                        }
                        return false;
                    }
                default:
                    {
                        return false;

                        //if (IsNSFW)
                        //{
                        //    url = Host.Pixiv.FilterID(url);
                        //    switch (url.Length)
                        //    {
                        //        case 5:
                        //            url = string.Format("https://www.wnacg.org/photos-index-aid-{0}.html", url);
                        //            break;
                        //        case 6:
                        //            url = string.Format("https://nhentai.net/g/{0}", url);
                        //            break;
                        //        case 7:
                        //            url = string.Format("https://hitomi.la/galleries/{0}.html", url);
                        //            break;
                        //        case 8:
                        //            url = string.Format("https://www.pixiv.net/member_illust.php?mode=medium&illust_id={0}", url);
                        //            break;
                        //        case 18:
                        //        case 19:
                        //            url = string.Format("https://exhentai.org/g/{0}", url);
                        //            break;
                        //        default:
                        //            return Task.FromResult(false);
                        //    }

                        //    ShowBookInfo(url, e);
                        //}                        
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
