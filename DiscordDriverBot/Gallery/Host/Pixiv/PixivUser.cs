using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace DiscordDriverBot.Gallery.Host.Pixiv
{
    public class PixivUserResponse
    {
        [JsonProperty("error")]
        public bool Error { get; set; }

        [JsonProperty("body")]
        public PixivUserBody Body { get; set; }
    }

    public class PixivUserBody
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("imageBig")]
        public string ImageBig { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("webpage")]
        public string Webpage { get; set; }

        [JsonProperty("background")]
        public PixivUserBackground Background { get; set; }
    }

    public class PixivUserBackground
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class PixivUserTopResponse
    {
        [JsonProperty("body")]
        public PixivUserTopBody Body { get; set; }
    }

    public class PixivUserTopBody
    {
        [JsonProperty("illusts")]
        public JToken Illusts { get; set; }
    }

    public class PixivWork
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("xRestrict")]
        public int XRestrict { get; set; }

        [JsonProperty("createDate")]
        public DateTime CreateDate { get; set; }
    }
}
