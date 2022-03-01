using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord_Driver_Bot.Gallery.Host.EHentai;

namespace Discord_Driver_Bot.HttpClients
{
    public class EHentaiAPIClient
    {
        HttpClient Client;
        CookieContainer Cookie = new CookieContainer();

        public EHentaiAPIClient()
        {
            Cookie.Add(new Cookie("ipb_member_id", Program.BotConfig.ExHentaiCookieMemberId, "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("ipb_member_id", Program.BotConfig.ExHentaiCookieMemberId, "/", ".exhentai.org"));
            Cookie.Add(new Cookie("ipb_pass_hash", Program.BotConfig.ExHentaiCookiePassHash, "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("ipb_pass_hash", Program.BotConfig.ExHentaiCookiePassHash, "/", ".exhentai.org"));
            Cookie.Add(new Cookie("sk", Program.BotConfig.ExHentaiCookieSK, "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("sk", Program.BotConfig.ExHentaiCookieSK, "/", ".exhentai.org"));
            Cookie.Add(new Cookie("nw", "1", "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("nw", "1", "/", ".exhentai.org"));

            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = Cookie;

            Client = new HttpClient(handler, true);
        }

        internal async Task<Stream> GetExHentaiDataAsync(string URL)
        {
            try
            {
                return await Client.GetStreamAsync(URL);
            }
            catch (Exception) { throw; }
        }

        public async Task<Gmetadata> GetGalleryMetadataAsync(int id, string token)
        {
            try
            {
                return (await GetGalleryMetadataAsync(new Dictionary<int, string> { { id, token } }))[0];
            }
            catch (Exception) { throw; }
        }

        public async Task<List<Gmetadata>> GetGalleryMetadataAsync(Dictionary<int, string> data)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("method");
                writer.WriteValue("gdata");
                writer.WritePropertyName("gidlist");
                writer.WriteStartArray();

                foreach (var item in data)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(item.Key);
                    writer.WriteValue(item.Value);
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
                writer.WritePropertyName("namespace");
                writer.WriteValue(1);
                writer.WriteEndObject();
            }

            try
            {
                return JsonConvert.DeserializeObject<GmetadataResponse>(await PostAPIDataAsync(sb.ToString())).gmetadata;
            }
            catch (Exception) { throw; }
        }

        public async Task<Tokenlist> GetGalleryTokenAsync(int id, string token, int page)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("method");
                writer.WriteValue("gtoken");
                writer.WritePropertyName("pagelist");
                writer.WriteStartArray();

                writer.WriteStartArray();
                writer.WriteValue(id);
                writer.WriteValue(token);
                writer.WriteValue(page);
                writer.WriteEndArray();

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            try
            {
                return JsonConvert.DeserializeObject<TokenlistResponse>(await PostAPIDataAsync(sb.ToString())).tokenlist[0];
            }
            catch (Exception) { throw; }
        }

        public static bool IsGalleryCanRead(string url)
        {
            bool isE_hentaiCanRead = false;

            try
            {
                IEnumerable<HtmlNode> htmlDocumentNode = new HtmlWeb().Load(url).DocumentNode.Descendants();
                isE_hentaiCanRead = !htmlDocumentNode.Any((x) => x.Name == "p" && x.InnerText.StartsWith("This gallery has been removed or is unavailable"));
            }
            catch (Exception) { }

            return isE_hentaiCanRead;
        }

        private async Task<string> PostAPIDataAsync(string data)
        {
            if (data == null)
                throw new ArgumentNullException("未包含Data值");

            try
            {
                byte[] byteArray = Encoding.Default.GetBytes(data);
                var responseMessage = await Client.PostAsync("https://api.e-hentai.org/api.php", new ByteArrayContent(byteArray));
                responseMessage.EnsureSuccessStatusCode();

                return await responseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception) { throw; }
        }
    }
}
