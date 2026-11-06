using FlareSolverrSharp.Types;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace DiscordDriverBot.HttpClients.Ascii2D
{
    //協助製作者Discord: `Yui__#5813`
    public class Ascii2DClient
    {
        private readonly BotConfig _botConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public Ascii2DClient(BotConfig botConfig, IHttpClientFactory httpClientFactory)
        {
            _botConfig = botConfig;
            _httpClientFactory = httpClientFactory;
        }

        public async IAsyncEnumerable<Result> FindAsync(string url)
        {
            var uri = new Uri("https://ascii2d.net/search/url/" + HttpUtility.UrlEncode(url));
            // FlareSolverr 認不出中文的 Cloudflare 驗證頁，只能等 25 秒讓 JS 驗證跑完
            var payload = JsonConvert.SerializeObject(new
            {
                cmd = "request.get",
                url = uri.ToString(),
                maxTimeout = 120000,
                waitInSeconds = 25
            });

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(180);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var apiUrl = _botConfig.FlareSolverrApiUrl.TrimEnd('/') + "/v1";
            using var response = await client.PostAsync(apiUrl, content);

            var result = JsonConvert.DeserializeObject<FlareSolverrResponse>(await response.Content.ReadAsStringAsync());
            if (result?.Status != "ok" || string.IsNullOrEmpty(result?.Solution?.Response))
                throw new HttpRequestException("FlareSolverr: " + result?.Message);

            foreach (var item in Parse(result.Solution.Response))
                yield return item;
        }

        public static IReadOnlyList<Result> Parse(string html)
        {
            var list = new List<Result>();
            HtmlDocument HTMLdoc = new();
            HTMLdoc.LoadHtml(html);

            var results = HTMLdoc.DocumentNode.SelectNodes("//div[@class='row item-box']");
            if (results == null)
                return list;

            foreach (var item in results)
            {
                var info = item.ChildNodes.FirstOrDefault((x) => x.HasClass("info-box"));

                if (info == null)
                    continue;

                var hash = info.SelectSingleNode("div[@class='hash']")?.InnerText ?? "";
                var detail = info.SelectSingleNode("div[@class='detail-box gray-link']");
                if (detail == null) continue;

                string thumbnail = "";
                try
                {
                    var imageBox = item.ChildNodes.First((x) => x.HasClass("image-box")).SelectSingleNode("img");
                    var src = imageBox.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src))
                        thumbnail = new Uri(new Uri("https://ascii2d.net/"), src).ToString();
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
                        title = strong + (extNode.FirstChild?.InnerText?.Trim('\n') ?? "");

                        if (nameAndAuthor == null) continue;

                        artlink = nameAndAuthor[0].Attributes["href"].Value;
                        host = $"{nameAndAuthor[0].InnerText}";

                        list.Add(new Result() { Hash = hash, URL = artlink, Author = author, Title = title, Host = host, Thumbnail = thumbnail });
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
                    host = detail.SelectSingleNode("h6/small")?.InnerText ?? host;
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

                list.Add(new Result() { Hash = hash, URL = artlink, Author = author, Title = title, Host = host, Thumbnail = thumbnail });
            }

            return list;
        }
    }
}
