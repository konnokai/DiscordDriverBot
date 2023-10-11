using Newtonsoft.Json.Linq;
using static DiscordDriverBot.HttpClients.SauceNAO.SauceNAOClient;

namespace DiscordDriverBot.HttpClients.SauceNAO
{
    public struct Result
    {
        /// <summary>
        /// Gets or sets the title of the artwork.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the author of the artwork.
        /// </summary>
        /// <value>
        /// The author.
        /// </value>
        public string Author { get; set; }
        /// <summary>
        /// Gets or sets the database of the artwork.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public string DB { get; set; }
        public SiteIndex Index { get; set; }
        public SourceRating Rating { get; set; }
        /// <summary>
        /// Gets or sets the thumbnail URL of the artwork.
        /// </summary>
        /// <value>
        /// The thumbnail.
        /// </value>
        public string Thumbnail { get; set; }
        /// <summary>
        /// Gets or sets the similarity of the artwork.
        /// </summary>
        /// <value>
        /// The similarity.
        /// </value>
        public double Similarity { get; set; }
        /// <summary>
        /// Gets or sets the sources of the artwork.
        /// </summary>
        /// <value>
        /// The sources.
        /// </value>
        public string Sources { get; set; }
        /// <summary>
        /// Gets or sets the raw data of the artwork.
        /// </summary>
        /// <value>
        /// The raw data.
        /// </value>
        public JToken RawData { get; set; }
    }
}
