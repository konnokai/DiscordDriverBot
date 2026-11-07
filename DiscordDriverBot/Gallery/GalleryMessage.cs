using Discord;
using System;
using System.Collections.Generic;
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

        // pixiv 的內文是 HTML，轉成 markdown 後還會留下 span 這類 Discord 不吃的標籤
        public static string CleanText(this string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string clean = text.Replace("\r\n", "\n").Replace('\r', '\n');
            clean = HtmlTag.Replace(clean, string.Empty);

            // 分隔線是幾百個 -，留著只會吃掉字數
            clean = Regex.Replace(clean, @"(?m)^[ \t]*[-—_=]{3,}[ \t]*$", string.Empty);
            return Regex.Replace(clean, @"\n{3,}", "\n\n").Trim();
        }

        // 只認已知的 HTML 標籤；作者自己寫的 <Fate/stay night> 不能被吃掉
        private static readonly Regex HtmlTag = new(
            @"</?(?:a|b|blockquote|br|center|code|dd|del|div|dl|dt|em|font|h[1-6]|hr|i|img|ins|li|mark|ol|p|pre|rp|rt|ruby|s|small|span|strike|strong|sub|sup|table|tbody|td|th|thead|tr|u|ul)\b[^>]*>",
            RegexOptions.IgnoreCase);
    }

    public sealed class GalleryCard
    {
        // CV2 訊息所有文字加起來的上限，超過 Discord 會整則退件
        public const int MaxTextLength = 4000;

        // 內文只留開頭，不然整張卡片都是作品說明
        public const int DescriptionLength = 200;

        // 留給 footer 的空間，footer 的 ID 是 !!Clear 找人用的，不能被擠掉
        private const int FooterReserve = 120;

        private static readonly Color AccentColor = new(0x00, 0xE5, 0x84);

        private readonly ContainerBuilder _container = new ContainerBuilder().WithAccentColor(AccentColor);
        private ActionRowBuilder _actionRow;
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

        public GalleryCard Text(string text, int limit)
            => AddText(TruncateMarkdown(UrlLinkText(text), limit), null);

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

        // 放 Container 外面的按鈕列，換頁用
        public GalleryCard WithActionRow(ActionRowBuilder actionRow)
        {
            _actionRow = actionRow;
            return this;
        }

        // CV2 沒有小尺寸圖片，Section 的頭像會跟作品縮圖一樣大，乾脆不放
        public GalleryCard WithFooter(IUser user)
            => AddText($"-# {user.Username} ID: {user.Id}", null, MaxTextLength);

        public MessageComponent Build()
        {
            var components = new List<IMessageComponentBuilder> { _container };
            if (_actionRow != null) components.Add(_actionRow);

            return new ComponentBuilderV2(components).Build();
        }

        public static ActionRowBuilder PagerRow(string prefix, string token, int page, int total)
        {
            int current = Math.Clamp(page, 1, total);
            string Id(string action, int target) => $"{prefix}:{token}:{action}:{target}";

            return new ActionRowBuilder()
                .WithButton("«", Id("first", 1), ButtonStyle.Secondary, disabled: current == 1)
                .WithButton("‹", Id("prev", current - 1), ButtonStyle.Secondary, disabled: current == 1)
                .WithButton($"{current}/{total}", Id("page", current), ButtonStyle.Secondary, disabled: true)
                .WithButton("›", Id("next", current + 1), ButtonStyle.Secondary, disabled: current == total)
                .WithButton("»", Id("last", total), ButtonStyle.Secondary, disabled: current == total);
        }

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
            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(target);
            }
            catch (UriFormatException)
            {
                return null;
            }

            // 先解開再找裡面的網址；絕對網址也會被同一個 regex 抓到
            // 不能先 Uri.TryCreate(target, Absolute)：Linux 會把 /jump.php 當成 Unix 路徑而提前回 null
            Match embedded = EmbeddedUrl.Match(decoded);
            if (!embedded.Success || !Uri.TryCreate(embedded.Value, UriKind.Absolute, out Uri url)) return null;

            return url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps ? url.AbsoluteUri : null;
        }

        private static string Truncate(string text, int max)
        {
            if (text.Length <= max) return text;

            int length = Math.Max(0, max - 1);
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
            return text[..length] + "…";
        }

        // 切在連結中間會留下半截 markdown，往回到連結開頭
        private static readonly Regex OpenLink = new(@"\[[^\[\]]*\]\([^)]*$|\[[^\[\]]*$");

        private static string TruncateMarkdown(string text, int max)
        {
            if (text.Length <= max) return text;

            int length = Math.Max(0, max - 1);
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;

            string cut = OpenLink.Replace(text[..length], string.Empty);

            // 剛好切到下一個粗體連結的開頭時，整行只剩 **，直接整行丟掉
            int lineStart = cut.LastIndexOf('\n') + 1;
            if (lineStart > 0 && cut[lineStart..].Trim(' ', '\t', '*', '_', '~').Length == 0)
                cut = cut[..lineStart];

            return cut.TrimEnd() + "…";
        }
    }
}
