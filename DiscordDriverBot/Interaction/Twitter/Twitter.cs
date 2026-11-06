using Discord;
using Discord.Interactions;
using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Interaction.Twitter
{
    public class Twitter : TopLevelModule
    {
        [SlashCommand("convert-to-vxtwitter", "將網址轉換成 vxTwitter")]
        public async Task ConvertToVxTwitter([Summary("url", "網址")] string url)
        {
            var fixedUrl = ToVxTwitter(url);
            await Context.Interaction.RespondAsync(fixedUrl, allowedMentions: AllowedMentions.None);
        }

        [MessageCommand("轉換網址成 vxTwitter")]
        public async Task ConvertMessageToVxTwitter(IMessage message)
        {
            var fixedMessage = ToVxTwitter(message.Content);
            await Context.Interaction.RespondAsync(fixedMessage, allowedMentions: AllowedMentions.None);
        }

        [MessageCommand("反解析推特圖片 ID")]
        public async Task DecodeTwitterImageAsync(IMessage message)
        {
            var result = BuildResult(message.Attachments.FirstOrDefault()?.Filename) ?? BuildResult(message.Content);
            if (result == null)
            {
                await Context.Interaction.SendErrorAsync("附件檔名或訊息文字不是推特圖片 ID");
                return;
            }

            await Context.Interaction.RespondAsync(result, allowedMentions: AllowedMentions.None);
        }

        public static string BuildResult(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            var name = fileName.Trim();
            // 輸入可能是完整網址或帶 :orig、副檔名，先還原成純 ID
            int cut = name.IndexOfAny(new[] { '?', '#' });
            if (cut >= 0) name = name.Substring(0, cut);
            name = name.Substring(name.LastIndexOf('/') + 1);
            name = Regex.Replace(name, @":[a-z]+\z", "", RegexOptions.IgnoreCase);
            name = Regex.Replace(name, @"\.[A-Za-z0-9]+\z", "");

            if (!Regex.IsMatch(name, @"^[A-Za-z0-9_-]{11,}\z")) return null;

            // X 把 8 bytes snowflake 編成 base64url，取前 11 碼就夠還原
            var encoded = name.Substring(0, 11).Replace('-', '+').Replace('_', '/') + "=";
            byte[] bytes;
            try { bytes = Convert.FromBase64String(encoded); }
            catch (FormatException) { return null; }
            if (bytes.Length < 8) return null;

            ulong id = BinaryPrimitives.ReadUInt64BigEndian(bytes);
            long ms = (long)(id >> 22) + 1288834974657L;
            if (ms < 1288834974657L || ms > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 86400000L) return null;

            const long window = 1800000L;
            long start = ms / window * window;
            long end = start + window;
            string ToUtcToken(long t) => DateTimeOffset.FromUnixTimeMilliseconds(t).UtcDateTime.ToString("yyyy-MM-dd_HH:mm:ss", CultureInfo.InvariantCulture) + "_UTC";
            string keyword = $"since:{ToUtcToken(start)} until:{ToUtcToken(end)} filter:images";
            string url = "https://x.com/search?q=" + Uri.EscapeDataString(keyword) + "&src=typed_query";

            var utc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
            var tw = utc.AddHours(8);
            string utcText = utc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string twText = tw.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return $"檔名：`{name}`\n" +
                   $"上傳時間：{utcText} UTC（台灣 {twText}）\n" +
                   $"搜尋關鍵字：`{keyword}`\n" +
                   Format.Url("開啟 X 搜尋頁", url);
        }

        private static string ToVxTwitter(string text)
        {
            text = Regex.Replace(text, @"(?<![\w.-])(?:(?:www|mobile)\.)?twitter\.com(?![\w-])", "vxtwitter.com", RegexOptions.IgnoreCase);
            return Regex.Replace(text, @"(?<![\w.-])(?:www\.)?x\.com(?![\w-])", "fixvx.com", RegexOptions.IgnoreCase);
        }
    }
}
