using Newtonsoft.Json;
using System.Collections.Generic;

namespace DiscordDriverBot.Gallery.Host.Pixiv
{
    public class Artist
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class PixivCat
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public Artist Artist { get; set; }

        [JsonProperty("multiple")]
        public bool Multiple { get; set; }

        [JsonProperty("original_url")]
        public string OriginalUrl { get; set; }

        [JsonProperty("original_urls")]
        public List<string> OriginalUrls { get; set; }

        [JsonProperty("original_url_proxy")]
        public string OriginalUrlProxy { get; set; }

        [JsonProperty("original_urls_proxy")]
        public List<string> OriginalUrlsProxy { get; set; }

        [JsonProperty("thumbnails")]
        public List<string> Thumbnails { get; set; }
    }
}
