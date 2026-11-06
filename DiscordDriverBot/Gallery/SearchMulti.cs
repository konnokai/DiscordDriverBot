using DiscordDriverBot.HttpClients.NHentai;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordDriverBot.Gallery
{

    public class SearchResult
    {
        public string SearchURL { get; set; }
        public int SearchCount { get; set; }
        public int TotalPages { get; set; }
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
                SearchResult searchResult = new SearchResult() { SearchURL = searchURL, SearchCount = searchCount, TotalPages = (searchCount / 25) + 1 };

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
            if (string.IsNullOrWhiteSpace(bookName)) return null;

            try
            {
                GallerySearch search = await Program.NHentaiAPIClient.SearchAsync(bookName, page);
                if (search?.Result == null || search.Result.Count == 0) return null;

                SearchResult searchResult = new()
                {
                    SearchURL = $"https://nhentai.net/search/?q={Uri.EscapeDataString(bookName)}&page={page}",
                    SearchCount = search.Total ?? search.Result.Count,
                    TotalPages = search.NumPages
                };

                foreach (GalleryListItem item in search.Result)
                {
                    searchResult.BookData.Add(new SearchBookData()
                    {
                        Title = item.EnglishTitle.Length > 256 ? item.EnglishTitle[..255] + "…" : item.EnglishTitle,
                        Language = SearchSingle.GetLanguage(item.TagIds),
                        URL = $"https://nhentai.net/g/{item.Id}/"
                    });
                }

                return searchResult;
            }
            catch (Exception ex) { Log.Error(ex.ToString()); return null; }
        }
    }
}
