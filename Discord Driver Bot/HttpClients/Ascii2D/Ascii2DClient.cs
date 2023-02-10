using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;

namespace Discord_Driver_Bot.HttpClients.Ascii2D
{
    //協助製作者Discord: `Yui__#5813`
    public class Ascii2DClient
    {
        public HttpClient Client { get; private set; }

        public Ascii2DClient(HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36 Edg/97.0.1072.62");
            Client = httpClient;
        }

        public async IAsyncEnumerable<Result> FindAsync(string url)
        {
            var encodedurl = HttpUtility.UrlEncode(url);
            var queryurl = "https://ascii2d.net/search/url/" + encodedurl;
            var rawhtml = await Client.GetStreamAsync(queryurl);
            HtmlDocument HTMLdoc = new HtmlDocument();
            HTMLdoc.Load(rawhtml, true);

            var results = HTMLdoc.DocumentNode.SelectNodes("/html/body/div/div/div/div[@class='row item-box']");
            if (results == null)
                yield return null;

            foreach (var item in results)
            {
                var info = item.SelectSingleNode("div[@class='col-xs-12 col-sm-12 col-md-8 col-xl-8 info-box']");
                var hash = info.SelectSingleNode("div[@class='hash']").InnerText;
                var detail = info.SelectSingleNode("div[@class='detail-box gray-link']");
                if (detail.ChildNodes.Count <= 1) continue;

                string thumbnail = "";
                try
                {
                    var imageBox = item.SelectSingleNode("div[@class='col-xs-12 col-sm-12 col-md-4 col-xl-4 text-xs-center image-box']").SelectSingleNode("img");
                    thumbnail = "https://ascii2d.net" + imageBox.GetAttributeValue("src", "");
                }
                catch (System.Exception) { }

                var strong = "";
                try
                {
                    var strongNode = detail.SelectSingleNode("strong");
                    if (strongNode != null)
                        strong = strongNode.InnerText + ": ";
                }
                catch (System.Exception) { }

                string host = "Unknown";
                string title = "";
                string author = "";
                string artlink = "";

                HtmlNodeCollection nameAndAuthor = null;
                if (!string.IsNullOrEmpty(strong))
                {
                    nameAndAuthor = detail.SelectNodes("div/a[@href]");

                    var extNode = detail.SelectSingleNode("div[@class='external']");
                    if (extNode != null)
                    {
                        title = strong + extNode.FirstChild.InnerText.Trim('\n');

                        if (nameAndAuthor == null) continue;

                        artlink = nameAndAuthor[0].Attributes["href"].Value;
                        host = $"{nameAndAuthor[0].InnerText}";

                        yield return new Result() { Hash = hash, URL = artlink, Author = author, Title = title, Host = host, Thumbnail = thumbnail };
                        continue;
                    }
                    else
                    {
                        var imgNode = detail.SelectSingleNode("div/img");
                        if (imgNode != null)
                            host = imgNode.GetAttributeValue("alt", "unknown").ToLower();
                    }
                }
                else
                {
                    nameAndAuthor = detail.SelectNodes("h6/a[@href]");
                    host = detail.SelectSingleNode("h6/small").InnerText;
                }

                if (nameAndAuthor == null) continue;

                for (int i = 0; i < nameAndAuthor.Count; i++)
                {
                    if (i == 0)
                    {
                        artlink = nameAndAuthor[i].Attributes["href"].Value;
                        title = $"{strong}{nameAndAuthor[i].InnerText}";
                    }
                    else if (i == 1)
                    {
                        author = nameAndAuthor[i].InnerText;
                    }
                }

                yield return new Result() { Hash = hash, URL = artlink, Author = author, Title = title, Host = host, Thumbnail = thumbnail };
            }
        }
    }
}
