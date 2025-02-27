using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDriverBot.HttpClients.SauceNAO
{
    public class SauceNAOClient
    {
        public HttpClient Client { get; private set; }

        /// <summary>Gets or sets the SauceNao API key.</summary>
        /// <value>The SauceNao API key.</value>
        public string ApiKey { get; set; }

        public SauceNAOClient(HttpClient httpClient, BotConfig botConfig)
        {
            Client = httpClient;
            ApiKey = botConfig.SauceNAOApiKey;
        }

        private static string SplitPascalCase(string convert)
        {
            return Regex.Replace(Regex.Replace(convert, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
        }

        public enum SourceRating
        {
            /// <summary>The image explicitness rating could not be determained.</summary>
            Unknown = 0,
            /// <summary>The image explicitness rating was determained to be safe, and contains no nudity.</summary>
            Safe = 1,
            /// <summary>The image explicitness rating was determained to be questionable, and could contain nudity.</summary>
            Questionable = 2,
            /// <summary>The image explicitness rating was determained to be NSFW, and contains nudity.</summary>
            Nsfw = 3
        }

        public enum SiteIndex
        {
            DoujinshiMangaLexicon = 3,
            Pixiv = 5,
            PixivArchive = 6,
            NicoNicoSeiga = 8,
            Danbooru = 9,
            Drawr = 10,
            Nijie = 11,
            Yandere = 12,
            OpeningsMoe = 13,
            FAKKU = 16,
            nHentai = 18,
            TwoDMarket = 19,
            MediBang = 20,
            AniDb = 21,
            IMDB = 23,
            Gelbooru = 25,
            Konachan = 26,
            SankakuChannel = 27,
            AnimePictures = 28,
            e621 = 29,
            IdolComplex = 30,
            BcyNetIllust = 31,
            BcyNetCosplay = 32,
            PortalGraphics = 33,
            DeviantArt = 34,
            Pawoo = 35,
            MangaUpdates = 36,
        }

        public class RateLimiter
        {
            public ushort UsesPerLimitCycle { get; private set; }
            public TimeSpan CycleLength { get; private set; }
            public DateTime LastCycleTime { get; private set; }
            public ushort CurrentUses { get; private set; }

            public bool IsLimited()
            {
                CurrentUses++; // Increment the amount of times we've used it this cycle

                // Have we gone into a new cycle?
                if (LastCycleTime.Add(CycleLength) < DateTime.Now)
                {
                    CurrentUses = 1; // A new cycle dawns, reset.
                    LastCycleTime = DateTime.Now;
                    return false; // Not limited anymore
                }

                // If we max out our uses this cycle
                if (CurrentUses >= UsesPerLimitCycle)
                    return true; // Limited

                return false; // If we get here, we're not limited
            }

            public RateLimiter(ushort usesPerCycle, TimeSpan cycleLength)
            {
                CurrentUses = 0;
                LastCycleTime = DateTime.Today;
                CycleLength = cycleLength;
                UsesPerLimitCycle = usesPerCycle;
            }
        }

        string _baseUrl = "https://saucenao.com/search.php";
        const int _defaultResultsCount = 8;

        /// <summary>Gets the sauce for the given image URI asynchronously.</summary>
        /// <param name="uri">The URI of the image.</param>
        /// <param name="resultsCount">The desired results count. If <see cref="ApiKey"/> is not set, the maximum is 16, otherwise it is 32. If the value exceeds the maximum, 6 results are returned.</param>
        /// <returns>A <see cref="IList{Result}"/> containing the results</returns>
        public async Task<IList<Result>> GetSauceAsync(string url, int resultsCount = _defaultResultsCount)
        {
            try
            {
                HttpResponseMessage response = await Client.PostAsync(_baseUrl, new MultipartFormDataContent
                    {
                        {new StringContent(ApiKey), "api_key"},
                        {new StringContent("2"), "output_type"},
                        {new StringContent(resultsCount.ToString()), "numres"},
                        {new StringContent("0"), "testmode"},
                        {new StringContent(url), "url"},
                        {new StringContent("999"), "db"}
                    });

                return await _parseResults(Client, JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync())["results"]);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        async Task<IList<Result>> _parseResults(HttpClient client, JToken results)
        {
            var rn = new List<Result>();

            foreach (var result in results)
            {
                rn.Add(await GetRating(client, new Result()
                {
                    Index = (SiteIndex)result["header"]["index_id"].ToObject<int>(),
                    DB = SplitPascalCase(((SiteIndex)result["header"]["index_id"].ToObject<int>()).ToString()),
                    Similarity = double.Parse(result["header"]["similarity"].ToString(), CultureInfo.InvariantCulture),
                    Thumbnail = result["header"]["thumbnail"].ToString(),
                    Author = _getAuthor(result["data"]),
                    Title = _getTitle(result["data"]),
                    Sources = result["data"]["ext_urls"]?.ToObject<List<string>>().First().Replace("https://www.pixiv.net/member_illust.php?mode=medium&illust_id=", "https://www.pixiv.net/artworks/"),
                    RawData = result
                }));
            }

            return rn;
        }

        string _getAuthor(JToken data)
        {
            var rn = data["member_name"] ?? data["author_name"] ?? data["creator"] ?? data["pawoo_user_display_name"] ?? data["author"];
            if (rn is JArray arr)
            {
                rn = arr.First;
            }
            return rn?.ToString();
        }

        string _getTitle(JToken data) => (data["title"] ?? data["jp_name"] ?? data["eng_name"] ?? data["material"] ?? data["source"])?.ToString();

        private async Task<Result> GetRating(HttpClient client, Result result)
        {
            async Task<Match> WebRequest(string url, string pattern)
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                HttpResponseMessage res = await client.GetAsync(url);
                Match webMatch = regex.Match((await res.Content.ReadAsStringAsync()));
                return webMatch;
            }

            // TODO: Test how effective the regex is without the backup urls
            Match match = null;
            switch (result.Index)
            {
                case SiteIndex.DoujinshiMangaLexicon:
                    match = await WebRequest(result.Sources, @"<td>.*?<b>Adult:<\/b><\/td><td>(.*)<\/td>");
                    if (match.Success)
                        result.Rating = match.Groups[1].Value == "Yes" ? SourceRating.Nsfw : SourceRating.Safe;
                    else result.Rating = SourceRating.Unknown;
                    break;

                case SiteIndex.Pixiv:
                case SiteIndex.PixivArchive:
                    string context = await (await client.GetAsync(result.Sources)).Content.ReadAsStringAsync();
                    result.Rating = (context.Contains(@"""tag"":""R-18""") ? SourceRating.Nsfw : SourceRating.Safe);
                    break;

                case SiteIndex.Gelbooru:
                case SiteIndex.Danbooru:
                case SiteIndex.SankakuChannel:
                case SiteIndex.IdolComplex:
                    match = await WebRequest(result.Sources, @"<li>Rating: (.*?)<\/li>");
                    if (!match.Success) result.Rating = SourceRating.Unknown;
                    else result.Rating = (SourceRating)Array.IndexOf(new[] { null, "Safe", "Questionable", "Explicit" }, match.Groups[1].Value);
                    break;

                case SiteIndex.Yandere:
                case SiteIndex.Konachan:
                    match = await WebRequest(result.Sources, @"<li>Rating: (.*?) <span class="".*?""><\/span><\/li>");
                    if (!match.Success) result.Rating = SourceRating.Unknown;
                    else result.Rating = (SourceRating)Array.IndexOf(new[] { null, "Safe", "Questionable", "Explicit" }, match.Groups[1].Value);
                    break;

                case SiteIndex.e621:
                    match = await WebRequest(result.Sources, @"<li>Rating: <span class="".*?"">(.*)<\/span><\/li>");
                    if (!match.Success) result.Rating = SourceRating.Unknown;
                    else result.Rating = (SourceRating)Array.IndexOf(new[] { null, "Safe", "Questionable", "Explicit" }, match.Groups[1].Value);
                    break;

                case SiteIndex.FAKKU:
                case SiteIndex.TwoDMarket:
                case SiteIndex.nHentai:
                    result.Rating = SourceRating.Nsfw;
                    break;

                case SiteIndex.DeviantArt:
                    match = await WebRequest(result.Sources, @"<h1>Mature Content<\/h1>");
                    result.Rating = match.Success ? SourceRating.Nsfw : SourceRating.Safe;
                    break;

                default:
                    result.Rating = SourceRating.Unknown;
                    break;
            }

            return result;
        }
    }
}
