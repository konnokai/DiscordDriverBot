using FlareSolverrSharp.Solvers;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace DiscordDriverBot.HttpClients.Ascii2D
{
    //協助製作者Discord: `Yui__#5813`
    public class Ascii2DClient
    {
        private readonly HttpClient _client;
        private readonly BotConfig _botConfig;

        public Ascii2DClient(IHttpClientFactory httpClientFactory, BotConfig botConfig)
        {
            _client = httpClientFactory.CreateClient();
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
            _botConfig = botConfig;
        }

        public async IAsyncEnumerable<Result> FindAsync(string url)
        {
            var uri = new Uri("https://ascii2d.net/search/url/" + HttpUtility.UrlEncode(url));
            var flareSolverr = new FlareSolverr(_botConfig.FlareSolverrApiUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var flareSolverrResponse = await flareSolverr.Solve(request);

            HtmlDocument HTMLdoc = new();
            HTMLdoc.LoadHtml(flareSolverrResponse.Solution.Response);

            var results = HTMLdoc.DocumentNode.SelectNodes("/html/body/div/div/div/div[@class='row item-box']");
            if (results == null)
                yield return null;

            foreach (var item in results)
            {
                var info = item.ChildNodes.FirstOrDefault((x) => x.HasClass("info-box"));

                if (info == null)
                    continue;

                var hash = info.SelectSingleNode("div[@class='hash']").InnerText;
                var detail = info.SelectSingleNode("div[@class='detail-box gray-link']");
                if (detail.ChildNodes.Count <= 1) continue;

                string thumbnail = "";
                try
                {
                    var imageBox = item.ChildNodes.First((x) => x.HasClass("image-box")).SelectSingleNode("img");
                    thumbnail = "https://ascii2d.net" + imageBox.GetAttributeValue("src", "");
                }
                catch (Exception) { }

                var strong = "";
                try
                {
                    var strongNode = detail.SelectSingleNode("strong");
                    if (strongNode != null)
                        strong = strongNode.InnerText + ": ";
                }
                catch (Exception) { }

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
