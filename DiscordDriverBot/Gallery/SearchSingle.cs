using DiscordDriverBot.HttpClients;
using DiscordDriverBot.HttpClients.NHentai;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordDriverBot.Gallery
{
    public static class SearchSingle
    {
        public static bool SearchNHentai(string bookName, out string url, out string language)
        {
            url = ""; language = "";

            if (string.IsNullOrWhiteSpace(bookName)) return false;

            try
            {
                List<GalleryListItem> results = Program.NHentaiAPIClient.SearchAsync(bookName).Result?.Result;
                if (results == null || results.Count == 0) return false;

                // 先比對書名，找不到再退回搜尋結果裡有中文或日文 tag 的
                List<GalleryListItem> candidates = results
                    .Where((x) => x.EnglishTitle.FormatBookName() == bookName || (x.JapaneseTitle ?? "").FormatBookName() == bookName)
                    .ToList();

                GalleryListItem pick = Pick(candidates) ?? Pick(results);
                if (pick == null) return false;

                url = $"https://nhentai.net/g/{pick.Id}/";
                language = GetLanguage(pick.TagIds);
                return true;
            }
            catch (Exception) { return false; }

            static GalleryListItem Pick(List<GalleryListItem> items)
                => items.FirstOrDefault((x) => x.TagIds.Contains(29963)) ?? items.FirstOrDefault((x) => x.TagIds.Contains(6346));
        }

        // nhentai 的語言 tag id：29963 中文、12227 英文、6346 日文
        public static string GetLanguage(List<long> tagIds)
        {
            if (tagIds.Contains(29963)) return "中文";
            if (tagIds.Contains(12227)) return "英文";
            if (tagIds.Contains(6346)) return "日文";
            return "其他";
        }

        public static bool SearchWnacg(string bookName, out string url, out string language)
        {
            url = ""; language = "";

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                var htmlDocumentNode = htmlWeb.Load($"https://www.wnacg.com/albums-index-page-1-sname-{bookName}.html").DocumentNode.Descendants().Where((x) => x.Name == "a" && x.ParentNode.Name == "div" && x.ParentNode.HasClass("pic_box"));
                foreach (HtmlNode item in htmlDocumentNode)
                {
                    if (item.GetAttributeValue("title", "").FormatBookName() == bookName)
                    {
                        if (item.Attributes.Any((x) => x.Name == "title" && (x.Value.Contains("汉化组") || x.Value.Contains("漢化組"))))
                        {
                            url = "https://www.wnacg.com" + item.Attributes["href"].Value;
                            language = "中文";
                            break;
                        }
                        else
                        {
                            url = "https://www.wnacg.com" + item.Attributes["href"].Value;
                            language = "日文";
                        }
                    }
                }

                if (url == "")
                {
                    foreach (HtmlNode item in htmlDocumentNode)
                    {
                        if (item.Attributes.Any((x) => x.Name == "title" && (x.Value.Contains("汉化组") || x.Value.Contains("漢化組"))))
                        {
                            url = "https://www.wnacg.com" + item.Attributes["href"].Value;
                            language = "中文";
                            break;
                        }
                        else
                        {
                            url = "https://www.wnacg.com" + item.Attributes["href"].Value;
                            language = "日文";
                        }
                    }
                }
            }
            catch (Exception) { }

            return url != "" && language != "";
        }

        public static bool SearchE_Hentai(string bookName, out string url, out string language)
        {
            url = ""; language = "";

            try
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.Load(Program.EHentaiAPIClient.GetExHentaiDataAsync($"https://e-hentai.org/?f_search={bookName}").Result);
                var htmlDocumentNode = htmlDocument.DocumentNode.Descendants().Where((x) => x.Name == "div" && x.HasClass("glink"));

                foreach (HtmlNode item in htmlDocumentNode)
                {
                    if (item.InnerText.FormatBookName() == bookName)
                    {
                        if (item.ParentNode.Name == "a")
                        {
                            url = item.ParentNode.GetAttributeValue("href", "");
                        }

                        if (item.InnerText.Contains("中国翻訳") || item.InnerText.Contains("中國翻訳"))
                        {
                            language = "中文";
                            break;
                        }
                        else language = "日文";
                    }
                }

                if (url == "")
                {
                    foreach (HtmlNode item in htmlDocumentNode)
                    {
                        if (item.ParentNode.Name == "a")
                        {
                            url = item.ParentNode.GetAttributeValue("href", "");
                        }

                        if (item.InnerText.Contains("中国翻訳") || item.InnerText.Contains("中國翻訳"))
                        {
                            language = "中文";
                            break;
                        }
                        else language = "日文";
                    }
                }

                if (!EHentaiAPIClient.IsGalleryCanRead(url)) language += "(需要銅星贊助)";
            }
            catch (Exception) { }

            return url != "" && language != "";
        }

        public static bool SearchExHentai(string bookName, out string url, out string language)
        {
            url = ""; language = "";

            try
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.Load(Program.EHentaiAPIClient.GetExHentaiDataAsync(string.Format("https://exhentai.org/?f_search={0}", bookName)).Result);
                var htmlDocumentNode = htmlDocument.DocumentNode.Descendants().Where((x) => x.Name == "div" && x.HasClass("glink"));

                foreach (HtmlNode item in htmlDocumentNode)
                {
                    if (item.InnerText.FormatBookName() == bookName)
                    {
                        if (item.ParentNode.Name == "a")
                        {
                            url = item.ParentNode.GetAttributeValue("href", "");
                        }

                        if (item.InnerText.Contains("中国翻訳") || item.InnerText.Contains("中國翻訳"))
                        {
                            language = "中文";
                            break;
                        }
                        else language = "日文";
                    }
                }

                if (url == "")
                {
                    foreach (HtmlNode item in htmlDocumentNode)
                    {
                        if (item.ParentNode.Name == "a")
                        {
                            url = item.ParentNode.GetAttributeValue("href", "");
                        }

                        if (item.InnerText.Contains("中国翻訳") || item.InnerText.Contains("中國翻訳"))
                        {
                            language = "中文";
                            break;
                        }
                        else language = "日文";
                    }
                }
            }
            catch (Exception) { }

            return url != "" && language != "";
        }
    }
}
