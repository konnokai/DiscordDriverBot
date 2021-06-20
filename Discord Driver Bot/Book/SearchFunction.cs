using HtmlAgilityPack;
using System;
using System.Linq;

namespace Discord_Driver_Bot.Book
{
    public static class SearchFunction
    {
        public static bool SearchNHentai(string bookName, out string url, out string language)
        {
            url = ""; language = "";

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                var htmlDocumentNode = htmlWeb.Load(string.Format("https://nhentai.net/search/?q={0}", bookName)).DocumentNode.Descendants().Where((x) => x.Name == "div" && x.HasClass("gallery"));
                foreach (HtmlNode item in htmlDocumentNode)
                {
                    if (item.Descendants().First((x) => x.HasClass("caption")).InnerText.FormatBookName() == bookName)
                    {
                        if (item.Attributes.Any((x) => x.Name == "data-tags" && x.Value.Contains("29963")))
                        {
                            url = "https://nhentai.net" + item.FirstChild.Attributes["href"].Value;
                            language = "中文";
                            break;
                        }
                        else if (item.Attributes.Any((x) => x.Name == "data-tags" && x.Value.Contains("6346")))
                        {
                            url = "https://nhentai.net" + item.FirstChild.Attributes["href"].Value;
                            language = "日文";
                        }
                    }
                }

                if (url == "")
                {
                    foreach (HtmlNode item in htmlDocumentNode)
                    {
                        if (item.Attributes.Any((x) => x.Name == "data-tags" && x.Value.Contains("29963")))
                        {
                            url = "https://nhentai.net" + item.FirstChild.Attributes["href"].Value;
                            language = "中文";
                            break;
                        }
                        else if (item.Attributes.Any((x) => x.Name == "data-tags" && x.Value.Contains("6346")))
                        {
                            url = "https://nhentai.net" + item.FirstChild.Attributes["href"].Value;
                            language = "日文";
                        }
                    }
                }
            }
            catch (Exception) { }
            
            return url != "" && language != "";
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
                htmlDocument.Load(Host.EHentai.API.GetExHentaiData($"https://e-hentai.org/?f_search={bookName}"));
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

                if (!Host.EHentai.API.IsGalleryCanRead(url)) language += "(需要銅星贊助)";
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
                htmlDocument.Load(Host.EHentai.API.GetExHentaiData(string.Format("https://exhentai.org/?f_search={0}", bookName)));
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
