using System.Collections.Generic;

namespace Discord_Driver_Bot.Gallery.Host.EHentai
{
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
