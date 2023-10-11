using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DiscordDriverBot.Gallery.Host.Pixiv
{
    public partial class IllustMetadata
    {
        public bool Error { get; set; }
        public string Message { get; set; }
        public Body Body { get; set; }
    }

    public class Body
    {
        [JsonProperty("illustId")]
        public string IllustId { get; set; }

        [JsonProperty("illustTitle")]
        public string IllustTitle { get; set; }

        [JsonProperty("illustComment")]
        public string IllustComment { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("illustType")]
        public int IllustType { get; set; }

        [JsonProperty("createDate")]
        public DateTime CreateDate { get; set; }

        [JsonProperty("uploadDate")]
        public DateTime UploadDate { get; set; }

        [JsonProperty("restrict")]
        public int Restrict { get; set; }

        [JsonProperty("xRestrict")]
        public int XRestrict { get; set; }

        [JsonProperty("sl")]
        public int Sl { get; set; }

        [JsonProperty("urls")]
        public Urls Urls { get; set; }

        [JsonProperty("tags")]
        public TagList Tags { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }

        [JsonProperty("storableTags")]
        public List<string> StorableTags { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("userAccount")]
        public string UserAccount { get; set; }

        [JsonProperty("likeData")]
        public bool LikeData { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("pageCount")]
        public int PageCount { get; set; }

        [JsonProperty("bookmarkCount")]
        public int BookmarkCount { get; set; }

        [JsonProperty("likeCount")]
        public int LikeCount { get; set; }

        [JsonProperty("commentCount")]
        public int CommentCount { get; set; }

        [JsonProperty("responseCount")]
        public int ResponseCount { get; set; }

        [JsonProperty("viewCount")]
        public int ViewCount { get; set; }

        [JsonProperty("bookStyle")]
        public string BookStyle { get; set; }

        [JsonProperty("isHowto")]
        public bool IsHowto { get; set; }

        [JsonProperty("isOriginal")]
        public bool IsOriginal { get; set; }

        [JsonProperty("imageResponseOutData")]
        public List<object> ImageResponseOutData { get; set; }

        [JsonProperty("imageResponseData")]
        public List<object> ImageResponseData { get; set; }

        [JsonProperty("imageResponseCount")]
        public int ImageResponseCount { get; set; }

        [JsonProperty("pollData")]
        public object PollData { get; set; }

        [JsonProperty("seriesNavData")]
        public object SeriesNavData { get; set; }

        [JsonProperty("descriptionBoothId")]
        public object DescriptionBoothId { get; set; }

        [JsonProperty("descriptionYoutubeId")]
        public object DescriptionYoutubeId { get; set; }

        [JsonProperty("comicPromotion")]
        public object ComicPromotion { get; set; }

        [JsonProperty("contestBanners")]
        public List<object> ContestBanners { get; set; }

        [JsonProperty("isBookmarkable")]
        public bool IsBookmarkable { get; set; }

        [JsonProperty("bookmarkData")]
        public object BookmarkData { get; set; }

        [JsonProperty("contestData")]
        public object ContestData { get; set; }

        [JsonProperty("isUnlisted")]
        public bool IsUnlisted { get; set; }

        [JsonProperty("request")]
        public object Request { get; set; }

        [JsonProperty("commentOff")]
        public int CommentOff { get; set; }

        [JsonProperty("aiType")]
        public int AiType { get; set; }
    }

    public partial class TagList
    {
        public long AuthorId { get; set; }
        public bool IsLocked { get; set; }
        [JsonProperty("tags")]
        public List<TagItem> Tags { get; set; }
        public bool Writable { get; set; }
    }

    public class TagItem
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }
    }

    public class Urls
    {
        [JsonProperty("mini")]
        public string Mini { get; set; }

        [JsonProperty("thumb")]
        public string Thumb { get; set; }

        [JsonProperty("small")]
        public string Small { get; set; }

        [JsonProperty("regular")]
        public string Regular { get; set; }

        [JsonProperty("original")]
        public string Original { get; set; }
    }
}