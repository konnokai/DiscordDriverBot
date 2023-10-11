using Newtonsoft.Json;

namespace DiscordDriverBot.SQLite.Table
{
    class BookData : DbEntity
    {
        public string URL { get; set; }
        public string DateTime { get; set; }
        public string Title { get; set; }
        public string ExtensionData { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Tags { get; set; }

        public BookData() { }

        public BookData(string url, string title, string extension_data, string thumbnail_url, object tags)
        {
            URL = url;
            DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Title = title;
            ExtensionData = extension_data;
            ThumbnailUrl = thumbnail_url;
            Tags = JsonConvert.SerializeObject(tags);
        }
    }
}
