using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;

namespace Discord_Driver_Bot.HttpClients.NHentai
{
    public class NHentaiAPIClient
    {
        Regex jsonRegex = new Regex(@"JSON\.parse\(""(?'JsonContext'{[^\""]+)");
        HttpClient Client;
        public NHentaiAPIClient()
        {
            Client = new HttpClient();
        }
        
        public async Task<Gallery> GetGalleryAsync(string id)
        {
            try
            {
                string web = await Client.GetStringAsync($"https://nhentai.net/g/{id}");
                var match = jsonRegex.Match(web);
                if (match.Success)
                {
                    var json = UnicodeToString(match.Groups["JsonContext"].Value);

                    return JsonConvert.DeserializeObject<Gallery>(json);
                }

                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        //http://trufflepenne.blogspot.com/2013/03/cunicode.html
        private string UnicodeToString(string srcText)
        {
            string dst = "";
            string src = srcText;
            int len = srcText.Length / 6;

            for (int i = 0; i <= len - 1; i++)
            {
                string str = "";
                str = src.Substring(0, 6).Substring(2);
                src = src.Substring(6);
                byte[] bytes = new byte[2];
                bytes[1] = byte.Parse(int.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                bytes[0] = byte.Parse(int.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                dst += Encoding.Unicode.GetString(bytes);
            }
            return dst;
        }
    }

    public class Gallery
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("media_id")]
        public long MediaId { get; set; }

        [JsonProperty("title")]
        public Title Title { get; set; }

        [JsonProperty("images")]
        public Images Images { get; set; }

        [JsonProperty("scanlator")]
        public string Scanlator { get; set; }

        [JsonProperty("upload_date")]
        public long UploadDate { get; set; }

        [JsonProperty("tags")]
        public List<Tag> Tags { get; set; }

        [JsonProperty("num_pages")]
        public long NumPages { get; set; }

        [JsonProperty("num_favorites")]
        public long NumFavorites { get; set; }
    }

    public class Images
    {
        [JsonProperty("pages")]
        public List<Cover> Pages { get; set; }

        [JsonProperty("cover")]
        public Cover Cover { get; set; }

        [JsonProperty("thumbnail")]
        public Cover Thumbnail { get; set; }
    }

    public class Cover
    {
        [JsonProperty("t")]
        public string T { get; set; }

        [JsonProperty("w")]
        public long W { get; set; }

        [JsonProperty("h")]
        public long H { get; set; }
    }

    public partial class Tag
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
}
