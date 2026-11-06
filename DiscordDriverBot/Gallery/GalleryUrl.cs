using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DiscordDriverBot.Gallery
{
    public enum BookHost
    {
        Wnacg, NHentai, E_Hentai, ExHentai, Hitomi, Pixiv, None
    }

    public enum GalleryLinkKind
    {
        Gallery, WnacgView, EHentaiPage, PixivUser
    }

    public enum GalleryShowResult { Shown, Blocked, Failed }

    public sealed record GalleryLink(BookHost Host, GalleryLinkKind Kind, string Id, string Token = null, int Page = 0, string SourceUrl = null)
    {
        public bool IsBook => Kind != GalleryLinkKind.PixivUser;
    }

    public static class GalleryUrl
    {
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        private static readonly Regex Candidate = new(@"(?<![#<|])https?://\S+", Options);
        private static readonly Regex AsciiUrl = new(@"(?<![#<|])https?://(?:(?!https?://)[a-z0-9\-._~:/?#@!$&*+,;=%])+", Options);

        public static bool TryParse(string url, out GalleryLink link)
        {
            link = null;

            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            string host = Regex.Replace(uri.Host, @"^(?:www|m)\.", "", Options);
            BookHost bookHost = host switch
            {
                "wnacg.com" or "wnacg.org" or "wnacg.ru" => BookHost.Wnacg,
                "e-hentai.org" => BookHost.E_Hentai,
                "exhentai.org" => BookHost.ExHentai,
                "hitomi.la" => BookHost.Hitomi,
                "pixiv.net" => BookHost.Pixiv,
                "nhentai.net" => BookHost.NHentai,
                _ => BookHost.None
            };

            if (bookHost == BookHost.None) return false;

            string path = uri.AbsolutePath;
            Match match;

            switch (bookHost)
            {
                case BookHost.Wnacg:
                    match = MatchPath(path, @"^/photos-(?:index|slide|list)-aid-(?<id>[0-9]{1,9})\.html$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.Gallery, match.Groups["id"].Value, url);

                    match = MatchPath(path, @"^/photos-index-page-[0-9]{1,6}-aid-(?<id>[0-9]{1,9})\.html$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.Gallery, match.Groups["id"].Value, url);

                    match = MatchPath(path, @"^/photos-view-id-(?<id>[0-9]{1,10})\.html$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.WnacgView, match.Groups["id"].Value, url);
                    break;

                case BookHost.E_Hentai:
                case BookHost.ExHentai:
                    match = MatchPath(path, @"^/g/(?<id>[0-9]{1,9})/(?<token>[0-9a-f]{10})/?$");
                    if (match.Success && TryNormalizeId(match.Groups["id"].Value, out string galleryId))
                    {
                        link = new GalleryLink(bookHost, GalleryLinkKind.Gallery, galleryId,
                            match.Groups["token"].Value.ToLowerInvariant(), 0, url);
                        return true;
                    }

                    match = MatchPath(path, @"^/s/(?<token>[0-9a-f]{10})/(?<id>[0-9]{1,9})-(?<page>[1-9][0-9]{0,4})/?$");
                    if (match.Success && TryNormalizeId(match.Groups["id"].Value, out string pageGalleryId))
                    {
                        link = new GalleryLink(bookHost, GalleryLinkKind.EHentaiPage, pageGalleryId,
                            match.Groups["token"].Value.ToLowerInvariant(), int.Parse(match.Groups["page"].Value), url);
                        return true;
                    }
                    break;

                case BookHost.Hitomi:
                    match = MatchPath(path, @"^/(?:galleries|reader)/(?<id>[0-9]{1,9})\.html$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.Gallery, match.Groups["id"].Value, url);

                    match = MatchPath(path, @"^/(?:doujinshi|manga|cg|gamecg|imageset)/(?:[^/]+-)?(?<id>[0-9]{1,9})\.html$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.Gallery, match.Groups["id"].Value, url);
                    break;

                case BookHost.Pixiv:
                    // shortcut: Pixiv 作品 ID 目前是 9 位數，超過時要改成 long
                    match = MatchPath(path, @"^/(?:[a-z]{2}(?:-[a-z]{2})?/)?artworks/(?<id>[0-9]{1,9})/?$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.Gallery, match.Groups["id"].Value, url);

                    match = MatchPath(path, @"^/(?:[a-z]{2}(?:-[a-z]{2})?/)?users/(?<id>[0-9]{1,9})(?:/[a-z]+)?/?$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.PixivUser, match.Groups["id"].Value, url);
                    break;

                case BookHost.NHentai:
                    match = MatchPath(path, @"^/g/(?<id>[0-9]{1,9})/?$");
                    if (match.Success) return SetLink(out link, bookHost, GalleryLinkKind.Gallery, match.Groups["id"].Value, url);
                    break;
            }

            return false;
        }

        public static List<GalleryLink> Extract(string text)
        {
            List<GalleryLink> links = new();
            if (string.IsNullOrWhiteSpace(text)) return links;

            // spoiler 和行內程式碼可能夾空白，先把整段移除
            text = Regex.Replace(text, @"\|\|[\s\S]*?\|\|", " ");
            text = Regex.Replace(text, @"`+[\s\S]*?`+", " ");

            HashSet<(BookHost, GalleryLinkKind, string, int)> seen = new();
            foreach (Match match in Candidate.Matches(text))
            {
                AddLinks(match.Value, links, seen);

                // 整段含中文時 Uri 會把路徑轉成 percent-encoding，抓不到就退回只取 ASCII 前段
                foreach (Match ascii in AsciiUrl.Matches(match.Value))
                    AddLinks(ascii.Value, links, seen);
            }

            return links;
        }

        private static void AddLinks(string value, List<GalleryLink> links, HashSet<(BookHost, GalleryLinkKind, string, int)> seen)
        {
            foreach (string part in value.Split(new[] { ',', ';', '，', '；' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = part.TrimEnd('.', ',', '!', '?', ';', ':', '*', '_', '~', '&', '。', '、', '）', '】', '》', '！', '？');
                if (!TryParse(candidate, out GalleryLink link)) continue;
                if (seen.Add(DedupKey(link))) links.Add(link);
            }
        }

        // E-Hentai 的 /g/ 和 /s/ 指向同一本，只留第一筆
        private static (BookHost, GalleryLinkKind, string, int) DedupKey(GalleryLink link)
        {
            BookHost host = link.Host == BookHost.E_Hentai ? BookHost.ExHentai : link.Host;
            if (host == BookHost.ExHentai) return (host, GalleryLinkKind.Gallery, link.Id, 0);

            return (host, link.Kind, link.Id, link.Page);
        }

        public static BookHost GuessGodSayHost(string godSay)
        {
            if (string.IsNullOrWhiteSpace(godSay)) return BookHost.None;
            if (Regex.IsMatch(godSay, @"^[0-9]{1,9}/[0-9a-f]{10}$", Options)) return BookHost.ExHentai;
            if (!Regex.IsMatch(godSay, @"^[0-9]+$", Options)) return BookHost.None;

            return godSay.Length switch
            {
                5 or 6 => BookHost.Wnacg,
                7 => BookHost.Hitomi,
                8 or 9 => BookHost.Pixiv,
                _ => BookHost.None
            };
        }

        public static string GodSayUrl(BookHost host, string godSay)
        {
            if (string.IsNullOrWhiteSpace(godSay)) return null;

            bool isEx = host is BookHost.E_Hentai or BookHost.ExHentai;
            bool isValid = isEx
                ? Regex.IsMatch(godSay, @"^[0-9]{1,9}/[0-9a-f]{10}$", Options)
                : Regex.IsMatch(godSay, @"^[0-9]{1,9}$", Options);
            if (!isValid) return null;

            return host switch
            {
                BookHost.Wnacg => $"https://www.wnacg.com/photos-index-aid-{godSay}.html",
                BookHost.E_Hentai or BookHost.ExHentai => $"https://exhentai.org/g/{godSay}",
                BookHost.Hitomi => $"https://hitomi.la/galleries/{godSay}.html",
                BookHost.NHentai => $"https://nhentai.net/g/{godSay}",
                BookHost.Pixiv => $"https://www.pixiv.net/artworks/{godSay}",
                _ => null
            };
        }

        private static bool SetLink(out GalleryLink link, BookHost host, GalleryLinkKind kind, string rawId, string sourceUrl)
        {
            link = null;
            if (!TryNormalizeId(rawId, out string id)) return false;

            link = new GalleryLink(host, kind, id, SourceUrl: sourceUrl);
            return true;
        }

        private static bool TryNormalizeId(string rawId, out string id)
        {
            id = null;
            if (!long.TryParse(rawId, NumberStyles.None, CultureInfo.InvariantCulture, out long value) || value <= 0)
                return false;

            id = value.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static Match MatchPath(string path, string pattern)
        {
            return Regex.Match(path, pattern, Options);
        }
    }
}
