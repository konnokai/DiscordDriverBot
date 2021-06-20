using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Discord_Driver_Bot.Book.Host.EHentai
{
    class API
    {
        static CookieContainer Cookie = new CookieContainer();

        static API()
        {
            Cookie.Add(new Cookie("ipb_member_id", Program.BotConfig.ExHentaiCookieMemberId, "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("ipb_member_id", Program.BotConfig.ExHentaiCookieMemberId, "/", ".exhentai.org"));
            Cookie.Add(new Cookie("ipb_pass_hash", Program.BotConfig.ExHentaiCookiePassHash, "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("ipb_pass_hash", Program.BotConfig.ExHentaiCookiePassHash, "/", ".exhentai.org"));
            Cookie.Add(new Cookie("sk", Program.BotConfig.ExHentaiCookieSK, "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("sk", Program.BotConfig.ExHentaiCookieSK, "/", ".exhentai.org"));
            Cookie.Add(new Cookie("nw", "1", "/", ".e-hentai.org"));
            Cookie.Add(new Cookie("nw", "1", "/", ".exhentai.org"));
        }

        internal static Stream GetExHentaiData(string URL)
        {
            try
            {
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create(URL);
                WR.CookieContainer = Cookie;
                WR.Method = "GET";
                return WR.GetResponse().GetResponseStream();
            }
            catch (Exception) { throw; }
        }

        public static Gmetadata GetGalleryMetadata(int id, string token)
        {
            try
            {
                return GetGalleryMetadata(new Dictionary<int, string> { { id, token } })[0];
            }
            catch (Exception) { throw; }
        }

        public static List<Gmetadata> GetGalleryMetadata(Dictionary<int, string> data)
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
                return JsonConvert.DeserializeObject<GmetadataResponse>(PostAPIData(sb.ToString())).gmetadata;
            }
            catch (Exception) { throw; }
        }

        public static Tokenlist GetGalleryToken(int id, string token, int page)
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
                return JsonConvert.DeserializeObject<TokenlistResponse>(PostAPIData(sb.ToString())).tokenlist[0];
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

        private static string PostAPIData(string data)
        {
            if (data == null)
                throw new ArgumentNullException("未包含Data值");

            try
            {
                byte[] byteArray = Encoding.Default.GetBytes(data);
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create("https://api.e-hentai.org/api.php");
                WR.Method = "POST";
                WR.ContentType = "application/x-www-form-urlencoded";
                WR.ContentLength = byteArray.Length;
                using (Stream dataStream = WR.GetRequestStream()) { dataStream.Write(byteArray, 0, byteArray.Length); }
                using (StreamReader SR = new StreamReader(WR.GetResponse().GetResponseStream())) { return SR.ReadToEnd(); }
            }
            catch (Exception) { throw; }
        }

        public class Gmetadata
        {
            public int gid { get; set; }
            public string token { get; set; }
            public string archiver_key { get; set; }
            public string title { get; set; }
            public string title_jpn { get; set; }
            public string category { get; set; }
            public string thumb { get; set; }
            public string uploader { get; set; }
            public string posted { get; set; }
            public string filecount { get; set; }
            public string filesize { get; set; }
            public bool expunged { get; set; }
            public string rating { get; set; }
            public string torrentcount { get; set; }
            public List<string> tags { get; set; }
        }

        public class GmetadataResponse
        {
            public List<Gmetadata> gmetadata { get; set; }
        }

        public class Tokenlist
        {
            public int gid { get; set; }
            public string token { get; set; }
        }

        public class TokenlistResponse
        {
            public List<Tokenlist> tokenlist { get; set; }
        }


        public class GalleryData
        {
            public int ID { get; set; }
            public string Token { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
            public string Post_Time { get; set; }
        }
    }
}
