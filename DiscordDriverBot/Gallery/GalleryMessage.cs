using Discord;
using System;
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

        public GalleryCard WithHeader(string title, string url, string thumbnailUrl = null)
        {
            string text = string.IsNullOrWhiteSpace(url)
                ? $"## {title.EscapeLinkText()}"
                : $"## [{title.EscapeLinkText()}]({url})";

            return AddText(text, thumbnailUrl);
        }

        public GalleryCard Text(string text) => AddText(text, null);

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

        public GalleryCard WithFooter(IUser user)
            => AddText($"-# {user.Username} ID: {user.Id}", user.GetAvatarUrl(), MaxTextLength);

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

        private static string Truncate(string text, int max)
        {
            if (text.Length <= max) return text;

            int length = Math.Max(0, max - 1);
            if (length > 0 && char.IsHighSurrogate(text[length - 1])) length--;
            return text[..length] + "…";
        }
    }
}
