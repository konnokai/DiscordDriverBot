using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.HttpClients.Hitomi
{
    public class HitomiAPIClient
    {
        HttpClient Client;
        public HitomiAPIClient()
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Referrer = new Uri("https://hitomi.la/");
        }

        public async Task<Gallery> GetGalleryAsync(string id)
        {
            try
            {
                var json = await Client.GetStringAsync($"https://ltn.hitomi.la/galleries/{id}.js");
                json = json.Substring(json.IndexOf('{'));

                return JsonConvert.DeserializeObject<Gallery>(json);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Tag
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("tag")]
        public string Name { get; set; }

        [JsonProperty("male")]
        public string Male { get; set; }

        [JsonProperty("female")]
        public string Female { get; set; }
    }

    public class Language
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("language_localname")]
        public string LanguageLocalname { get; set; }

        [JsonProperty("galleryid")]
        public string Galleryid { get; set; }
    }

    public class Artist
    {
        [JsonProperty("artist")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class File
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("haswebp")]
        public int Haswebp { get; set; }

        [JsonProperty("hasavif")]
        public int Hasavif { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class Gallery
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("parodys")]
        public object Parodys { get; set; }

        [JsonProperty("tags")]
        public List<Tag> Tags { get; set; }

        [JsonProperty("video")]
        public object Video { get; set; }

        [JsonProperty("characters")]
        public object Characters { get; set; }

        [JsonProperty("languages")]
        public List<Language> Languages { get; set; }

        [JsonProperty("artists")]
        public List<Artist> Artists { get; set; }

        [JsonProperty("scene_indexes")]
        public List<object> SceneIndexes { get; set; }

        [JsonProperty("language_url")]
        public string LanguageUrl { get; set; }

        [JsonProperty("groups")]
        public object Groups { get; set; }

        [JsonProperty("japanese_title")]
        public object JapaneseTitle { get; set; }

        [JsonProperty("related")]
        public List<int> Related { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("files")]
        public List<File> Files { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("videofilename")]
        public object Videofilename { get; set; }

        [JsonProperty("language_localname")]
        public string LanguageLocalname { get; set; }
    }


}
