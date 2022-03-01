using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Gallery
{

    public  class SearchResult
    {
        public string SearchURL { get; set; }
        public int SearchCount { get; set; }
        public List<SearchBookData> BookData { get; set; } = new List<SearchBookData>();
    }

    public class SearchBookData
    {
        public string Title { get; set; }
        public string Language { get; set; }
        public string URL { get; set; }
    }

    public static class SearchMulti
    {
        public static async Task<SearchResult> SearchExHentai(string bookName, int page)
        {
            page--;
            try
            {
                string searchURL = $"https://exhentai.org/?f_search={bookName}&advsearch=1&f_sname=on&f_stags=on&f_sh=on&f_spf=&f_spt=".Replace(" ", "+");
                if (page > 0) searchURL += "&page=" + page.ToString();

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.Load(await Program.EHentaiAPIClient.GetExHentaiDataAsync(searchURL));
                var htmlDocumentNode = htmlDocument.DocumentNode.Descendants();

                IEnumerable<HtmlNode> htmlDocumentNode1 = htmlDocumentNode.Where((x) => x.HasClass("glink"));
                int searchCount = int.Parse(htmlDocumentNode.First((x) => x.HasClass("ip")).InnerText.Split(new char[] { ' ' })[1].Replace(",", ""));
                SearchResult searchResult = new SearchResult() { SearchURL = searchURL, SearchCount = searchCount };

                foreach (HtmlNode item in htmlDocumentNode1)
                {
                    string language = "";
                    if (item.InnerText.Contains("中国翻訳") || item.InnerText.Contains("中國翻訳") || item.InnerText.Contains("中國語")) language = "中文";
                    else if (item.InnerText.Contains("英訳")) language = "英文";
                    else if (item.InnerText.Contains("訳")) language = "其他";
                    else language = "日文";

                    searchResult.BookData.Add(new SearchBookData() { Title = item.InnerText, Language = language, URL = item.ParentNode.GetAttributeValue("href", "") });
                }

                return searchResult;
            }
            catch (Exception ex) { Log.Error(ex.ToString()); return null; }
        }
        public static async Task<SearchResult> SearchNHentaiAsync(string bookName, int page)
        {
            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                string searchURL = string.Format("https://nhentai.net/search/?q={0}", bookName).Replace(" ", "+");
                if (page > 1) searchURL += "&page=" + page.ToString();

                IEnumerable<HtmlNode> htmlDocumentNode = (await htmlWeb.LoadFromWebAsync(searchURL)).DocumentNode.Descendants();
                IEnumerable<HtmlNode> htmlNodes = htmlDocumentNode.Where((x) => x.Name == "div" && x.HasClass("gallery"));
                int searchCount = int.Parse(htmlDocumentNode.First((x) => x.Name == "h2").InnerText.Split(new char[] { ' ' })[0]);
                SearchResult searchResult = new SearchResult() { SearchURL = searchURL, SearchCount = searchCount };

                foreach (HtmlNode item in htmlNodes)
                {
                    if (item.Attributes.Any((x) => x.Name == "data-tags"))
                    {
                        string language = "";
                        if (item.Attributes.Any((x) => x.Value.Contains("29963"))) language = "中文";
                        else if (item.Attributes.Any((x) => x.Value.Contains("6346"))) language = "日文";
                        else language = "其他";

                        searchResult.BookData.Add(new SearchBookData() { Title = item.Descendants().First((x) => x.HasClass("caption")).InnerText, Language = language, URL = "https://nhentai.net" + item.FirstChild.Attributes["href"].Value });
                    }
                }

                return searchResult;
            }
            catch (Exception ex) { Log.Error(ex.ToString()); return null; }
        }
    }
}