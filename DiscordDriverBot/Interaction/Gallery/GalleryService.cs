using Discord;
using DiscordDriverBot.HttpClients.Ascii2D;
using DiscordDriverBot.HttpClients.SauceNAO;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordDriverBot.Interaction.Gallery
{
    public class GalleryService : IInteractionService
    {
        internal string[] AllowedFileTypes { get; } = new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".svg", ".webp" };
        private Ascii2DClient _ascii2DClient;
        private SauceNAOClient _sauceNAOClient;
        private IHttpClientFactory _httpClientFactory;

        public GalleryService(Ascii2DClient ascii2DClient, SauceNAOClient sauceNAOClient, IHttpClientFactory httpClientFactory)
        {
            _ascii2DClient = ascii2DClient;
            _sauceNAOClient = sauceNAOClient;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(string ErrorMessage, Embed Embed)> SauceFromAscii2DAsync(string url)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var req = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                req.EnsureSuccessStatusCode();

                if (req.Content.Headers.ContentLength > 10485760)
                {
                    return ("圖檔不可大於 10 MB", null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), url);
                return ("搜尋失敗，未知的錯誤", null);
            }

            try
            {
                List<string> description = new List<string>();
                DiscordDriverBot.HttpClients.Ascii2D.Result first = null;
                await foreach (var item in _ascii2DClient.FindAsync(url).Take(3))
                {
                    first ??= item;
                    if (item.Host == "dlsite")
                        description.Add($"{Format.Url(item.Host, item.URL)} {item.Title}");
                    else
                        description.Add($"{Format.Url(item.Host, item.URL)} {item.Title} ({item.Author})");
                }

                if (first == null)
                    return ("Ascii2D 沒拿到搜尋頁，可能卡在網站驗證，請稍後再試", null);

                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(first.Title)
                    .WithDescription(string.Join('\n', description))
                    .WithThumbnailUrl(first.Thumbnail)
                    .WithFooter("Ascii2D");

                return (null, embedBuilder.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), url);
                return ("搜尋失敗，未知的錯誤", null);
            }
        }

        public async Task<(string ErrorMessage, Embed Embed)> SauceFromSauceNAOAsync(string url)
        {
            try
            {
                var sauceResult = await _sauceNAOClient.GetSauceAsync(url).ConfigureAwait(false);
                if (sauceResult != null)
                {
                    List<string> description = new List<string>();
                    foreach (var item in sauceResult)
                    {
                        if (item.Index == SauceNAOClient.SiteIndex.nHentai) description.Add($"NHentai {item.Similarity}% 相似度");
                        else if (item.Sources != null)
                        {
                            description.Add($"[{item.DB}]({item.Sources}) {item.Similarity}% 相似度");
                            try
                            {
                                if (item.Index == SauceNAOClient.SiteIndex.Danbooru)
                                {
                                    var htmlweb = new HtmlWeb()
                                    {
                                        UserAgent = "DanbooruFetcher"
                                    };

                                    var DOM = htmlweb.Load(item.Sources);
                                    var sourceNode = DOM.GetElementbyId("post-info-source");
                                    var sourceUrlNode = sourceNode.SelectSingleNode("a");

                                    //有可能沒有Source
                                    if (sourceUrlNode is not null)
                                    {
                                        var sourceUrl = sourceUrlNode.GetAttributeValue("href", "");
                                        if (!string.IsNullOrEmpty(sourceUrl))
                                            description.Add($"[Danbooru 來源網址]({sourceUrl})");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), "Danbooru 解析失敗");
                            }
                        }
                    }

                    EmbedBuilder embedBuilder = new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(sauceResult[0].Title)
                        .WithDescription(string.Join('\n', description))
                        .WithThumbnailUrl(sauceResult[0].Thumbnail)
                        .WithFooter("SauceNAO");

                    return (null, embedBuilder.Build());
                }
                else
                {
                    return ("搜尋失敗，無回傳值", null);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), url);
                return ("搜尋失敗，未知的錯誤", null);
            }
        }
    }
}