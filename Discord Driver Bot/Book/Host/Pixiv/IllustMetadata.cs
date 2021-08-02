using System;
using System.Collections.Generic;

namespace Discord_Driver_Bot.Book.Host.Pixiv
{
    public partial class IllustMetadata
    {
        public bool Error { get; set; }
        public string Message { get; set; }
        public Body Body { get; set; }
    }

    public partial class Body
    {
        public long IllustId { get; set; }
        public string IllustTitle { get; set; }
        public string IllustComment { get; set; }
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long IllustType { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset UploadDate { get; set; }
        public long Restrict { get; set; }
        public long XRestrict { get; set; }
        public long Sl { get; set; }
        public Urls Urls { get; set; }
        public TagList Tags { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public long PageCount { get; set; }
        public long BookmarkCount { get; set; }
        public long LikeCount { get; set; }
    }

    public partial class BookmarkData
    {
        public string Id { get; set; }
        public bool Private { get; set; }
    }

    public partial class TagList
    {
        public long AuthorId { get; set; }
        public bool IsLocked { get; set; }
        public List<TagItem> Tags { get; set; }
        public bool Writable { get; set; }
    }

    public partial class TagItem
    {
        public string Tag { get; set; }
        public bool Locked { get; set; }
        public bool Deletable { get; set; }
        public long? UserId { get; set; }
        public string Romaji { get; set; }
        public Translation Translation { get; set; }
        public string UserName { get; set; }
    }

    public partial class Translation
    {
        public string En { get; set; }
    }

    public partial class Urls
    {
        public string Mini { get; set; }
        public string Thumb { get; set; }
        public string Small { get; set; }
        public string Regular { get; set; }
        public string Original { get; set; }
    }
}