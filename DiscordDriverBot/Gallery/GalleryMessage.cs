using Discord;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Gallery
{
    // 畫廊卡片統一走 Components V2，才有 MediaGallery 這類 embed 做不到的排版
    public static class GalleryMessage
    {
        public static GalleryCard Card() => new();

        public static string EscapeLinkText(this string text) =>
            (text ?? string.Empty)
                .Replace("\\", "＼")
                .Replace("[", "［")
                .Replace("]", "］")
                .Replace("\r", string.Empty)
                .Replace("\n", " ");
    }

    public sealed class GalleryCard
    {
        // CV2 訊息所有文字加起來的上限，超過 Discord 會整則退件
        public const int MaxTextLength = 4000;

        // 留給 footer 的空間，footer 的 ID 是 !!Clear 找人用的，不能被擠掉
        private const int FooterReserve = 120;

        private static readonly Color AccentColor = new(0x00, 0xE5, 0x84);

        private readonly ContainerBuilder _container = new ContainerBuilder().WithAccentColor(AccentColor);
        private int _used;

        public GalleryCard WithHeader(string title, string url, string thumbnailUrl = null, string creator = null)
        {
            string byline = string.IsNullOrWhiteSpace(creator) ? "" : $"-# {creator.EscapeLinkText()}\n";

            string text = string.IsNullOrWhiteSpace(url)
                ? $"{byline}{title.EscapeLinkText()}"
                : $"{byline}[{title.EscapeLinkText()}]({url})";

            return AddText(text, thumbnailUrl);
        }

        public GalleryCard Text(string text) => AddText(UrlLinkText(text), null);

        public GalleryCard WithField(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return this;

            // "**名稱**\n" 本身也要算進上限
            string prefix = $"**{name}**\n";
            int budget = MaxTextLength - FooterReserve - _used - prefix.Length;
            if (budget <= 0) return this;

            return AddText(prefix + Truncate(value, budget), null);
        }

        public GalleryCard WithMediaGallery(MediaGalleryBuilder gallery)
        {
            if (gallery.Items.Count > 0)
                _container.AddComponent(gallery);

            return this;
        }

        // CV2 沒有小尺寸圖片，Section 的頭像會跟作品縮圖一樣大，乾脆不放
        public GalleryCard WithFooter(IUser user)
            => AddText($"-# {user.Username} ID: {user.Id}", null, MaxTextLength);

        public MessageComponent Build()
            => new ComponentBuilderV2(new[] { (IMessageComponentBuilder)_container }).Build();

        public async Task SendAsync(IMessageChannel messageChannel, IInteractionContext interactionContext)
        {
            MessageComponent built = Build();

            // 卡片內容來自外部網站，不能讓文字裡的 @ 真的標記人
            if (interactionContext == null)
                await messageChannel.SendMessageAsync(components: built, allowedMentions: AllowedMentions.None, flags: MessageFlags.ComponentsV2);
            else
                await interactionContext.Interaction.FollowupAsync(components: built, allowedMentions: AllowedMentions.None, flags: MessageFlags.ComponentsV2);
        }

        private GalleryCard AddText(string text, string thumbnailUrl, int limit = MaxTextLength - FooterReserve)
        {
            int budget = limit - _used;
            if (budget <= 0) return this;

            text = Truncate(text, budget);
            _used += text.Length;

            if (string.IsNullOrWhiteSpace(thumbnailUrl))
                _container.AddComponent(new TextDisplayBuilder(text));
            else
                _container.AddComponent(new SectionBuilder()
                    .WithComponents(new[] { new TextDisplayBuilder(text) })
                    .WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(thumbnailUrl))));

            return this;
        }

        // 連結文字本身就是網址時 Discord 會防釣魚，整串 [網址](網址) 原文照印，改成顯示網域
        private static readonly Regex UrlLink = new(@"\[https?://[^\]\s]+\]\((?<target>[^)\s]+)\)", RegexOptions.IgnoreCase);
        private static readonly Regex EmbeddedUrl = new(@"https?://[^\s""'<>]+", RegexOptions.IgnoreCase);

        private static string UrlLinkText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return UrlLink.Replace(text, (match) =>
            {
                string url = ResolveLinkTarget(match.Groups["target"].Value);
                return url == null ? match.Value : $"[{new Uri(url).Host}]({url})";
            });
        }

        // Pixiv 把站外連結換成 /jump.php?https%3A%2F%2F...，要還原成真正的網址才點得動
        private static string ResolveLinkTarget(string target)
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out Uri absolute))
            {
                return absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps
                    ? absolute.AbsoluteUri
                    : null;
            }

            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(target);
            }
            catch (UriFormatException)
            {
                return null;
            }

            Match embedded = EmbeddedUrl.Match(decoded);
            return embedded.Success && Uri.TryCreate(embedded.Value, UriKind.Absolute, out Uri url) ? url.AbsoluteUri : null;
        }

        private static string Truncate(string text, int max)
        {
            if (text.Length <= max) return text;

            int length = Math.Max(0, max - 1);
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
            return text[..length] + "…";
        }
    }
}
