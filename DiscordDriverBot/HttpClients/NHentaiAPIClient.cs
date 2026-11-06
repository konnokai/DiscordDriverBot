using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordDriverBot.HttpClients.NHentai
{
    // nhentai 官方 API v2，規格在 https://nhentai.net/api/v2/docs
    public class NHentaiAPIClient
    {
        private const string ApiBase = "https://nhentai.net/api/v2";

        private static readonly HttpClient Client = CreateClient();

        private readonly string _apiKey;

        public NHentaiAPIClient(string apiKey = null)
        {
            _apiKey = apiKey;
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
            // API 文件要求帶可辨識的 User-Agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordDriverBot/1.0 (+https://github.com/konnokai/DiscordDriverBot)");
            return client;
        }

        public Task<Gallery> GetGalleryAsync(string id)
            => GetAsync<Gallery>($"/galleries/{id}");

        public Task<GallerySearch> SearchAsync(string keyword, int page = 1)
            => GetAsync<GallerySearch>($"/search?query={Uri.EscapeDataString(keyword)}&page={page}");

        private async Task<T> GetAsync<T>(string path)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, ApiBase + path);

            // 有 Key 額度比較高（搜尋 10→20 次/分），沒填就維持匿名
            if (!string.IsNullOrWhiteSpace(_apiKey))
                request.Headers.Add("Authorization", $"Key {_apiKey}");

            using HttpResponseMessage response = await Client.SendAsync(request);

            // 404 交給呼叫端當「找不到本子」，不要當例外
            if (response.StatusCode == HttpStatusCode.NotFound) return default;

            response.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
    }

    public class Gallery
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public Title Title { get; set; }

        [JsonProperty("thumbnail")]
        public CoverInfo Thumbnail { get; set; }

        [JsonProperty("upload_date")]
        public long UploadDate { get; set; }

        [JsonProperty("tags")]
        public List<Tag> Tags { get; set; }

        [JsonProperty("num_pages")]
        public long NumPages { get; set; }

        [JsonProperty("num_favorites")]
        public long NumFavorites { get; set; }
    }

    public class CoverInfo
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }
    }

    public class Tag
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }
    }

    public class Title
    {
        [JsonProperty("english")]
        public string English { get; set; }

        [JsonProperty("japanese")]
        public string Japanese { get; set; }

        [JsonProperty("pretty")]
        public string Pretty { get; set; }
    }

    public class GallerySearch
    {
        [JsonProperty("result")]
        public List<GalleryListItem> Result { get; set; }

        [JsonProperty("num_pages")]
        public int NumPages { get; set; }

        [JsonProperty("per_page")]
        public int PerPage { get; set; }

        [JsonProperty("total")]
        public int? Total { get; set; }
    }

    public class GalleryListItem
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("english_title")]
        public string EnglishTitle { get; set; }

        [JsonProperty("japanese_title")]
        public string JapaneseTitle { get; set; }

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonProperty("num_pages")]
        public int NumPages { get; set; }

        [JsonProperty("num_favorites")]
        public int NumFavorites { get; set; }

        [JsonProperty("tag_ids")]
        public List<long> TagIds { get; set; } = new();
    }
}
