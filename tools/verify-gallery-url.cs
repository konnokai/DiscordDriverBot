#:project ../DiscordDriverBot/DiscordDriverBot.csproj
#:property PublishAot=false
#nullable disable

using DiscordDriverBot.Gallery;

int failed = 0;

void Expect(string input, BookHost host, GalleryLinkKind kind, string id, string token = null, int page = 0)
{
    if (!GalleryUrl.TryParse(input, out GalleryLink link))
    {
        Console.WriteLine($"FAIL parse: {input} -> no result");
        failed++;
        return;
    }

    if (link.Host != host || link.Kind != kind || link.Id != id || link.Token != token || link.Page != page)
    {
        Console.WriteLine($"FAIL parse: {input} -> {link.Host}/{link.Kind}/{link.Id}/{link.Token}/{link.Page}, expected {host}/{kind}/{id}/{token}/{page}");
        failed++;
    }
}

void Reject(string input)
{
    if (GalleryUrl.TryParse(input, out _))
    {
        Console.WriteLine($"FAIL reject: {input}");
        failed++;
    }
}

void ExpectCount(string text, int expected)
{
    int actual = GalleryUrl.Extract(text).Count;
    if (actual != expected)
    {
        Console.WriteLine($"FAIL extract: {text} -> {actual}, expected {expected}");
        failed++;
    }
}

void ExpectGuess(string input, BookHost host)
{
    BookHost actual = GalleryUrl.GuessGodSayHost(input);
    if (actual != host)
    {
        Console.WriteLine($"FAIL guess: {input} -> {actual}, expected {host}");
        failed++;
    }
}

void ExpectGodSay(BookHost host, string godSay, bool valid)
{
    string url = GalleryUrl.GodSayUrl(host, godSay);
    if (!valid)
    {
        if (url != null)
        {
            Console.WriteLine($"FAIL godsay reject: {host} {godSay} -> {url}");
            failed++;
        }
        return;
    }

    if (url == null || !GalleryUrl.TryParse(url, out _))
    {
        Console.WriteLine($"FAIL godsay: {host} {godSay} -> {url}");
        failed++;
    }
}

Expect("https://wnacg.com/photos-slide-aid-319097.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "319097");
Expect("https://www.wnacg.com/photos-index-aid-319097.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "319097");
Expect("https://www.wnacg.com/photos-index-page-2-aid-319097.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "319097");
Expect("https://www.wnacg.com/photos-view-id-26765686.html", BookHost.Wnacg, GalleryLinkKind.WnacgView, "26765686");
Expect("https://www.wnacg.com/photos-list-aid-319097.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "319097");
Expect("https://m.wnacg.ru/photos-index-aid-319097.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "319097");
Expect("https://e-hentai.org/g/123456/abcdef1234/", BookHost.E_Hentai, GalleryLinkKind.Gallery, "123456", "abcdef1234");
Expect("https://exhentai.org/g/123456/abcdef1234", BookHost.ExHentai, GalleryLinkKind.Gallery, "123456", "abcdef1234");
Expect("https://e-hentai.org/s/abcdef1234/123456-3", BookHost.E_Hentai, GalleryLinkKind.EHentaiPage, "123456", "abcdef1234", 3);
Expect("https://hitomi.la/galleries/1234567.html", BookHost.Hitomi, GalleryLinkKind.Gallery, "1234567");
Expect("https://hitomi.la/reader/1234567.html#3", BookHost.Hitomi, GalleryLinkKind.Gallery, "1234567");
Expect("https://hitomi.la/doujinshi/test-1984337.html", BookHost.Hitomi, GalleryLinkKind.Gallery, "1984337");
Expect("https://hitomi.la/doujinshi/日本語-1984337.html", BookHost.Hitomi, GalleryLinkKind.Gallery, "1984337");
Expect("https://hitomi.la/doujinshi/%E6%97%A5%E6%9C%AC%E8%AA%9E-1984337.html", BookHost.Hitomi, GalleryLinkKind.Gallery, "1984337");
Expect("https://hitomi.la/manga/some-2-title-1984337.html", BookHost.Hitomi, GalleryLinkKind.Gallery, "1984337");
Expect("https://hitomi.la/cg/1984337.html", BookHost.Hitomi, GalleryLinkKind.Gallery, "1984337");
Expect("https://www.pixiv.net/artworks/123456789", BookHost.Pixiv, GalleryLinkKind.Gallery, "123456789");
Expect("https://www.pixiv.net/en/artworks/123456789?p=1", BookHost.Pixiv, GalleryLinkKind.Gallery, "123456789");
Expect("https://www.pixiv.net/users/18007162", BookHost.Pixiv, GalleryLinkKind.PixivUser, "18007162");
Expect("https://www.pixiv.net/en/users/18007162/artworks", BookHost.Pixiv, GalleryLinkKind.PixivUser, "18007162");
Expect("https://www.pixiv.net/zh-tw/artworks/123456789", BookHost.Pixiv, GalleryLinkKind.Gallery, "123456789");
Expect("https://www.pixiv.net/zh-tw/users/18007162", BookHost.Pixiv, GalleryLinkKind.PixivUser, "18007162");
Expect("https://nhentai.net/g/123456/", BookHost.NHentai, GalleryLinkKind.Gallery, "123456");
Expect("HTTPS://WWW.PIXIV.NET/ARTWORKS/123456789", BookHost.Pixiv, GalleryLinkKind.Gallery, "123456789");
Expect("https://www.wnacg.com/photos-index-aid-06836.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "6836");
Expect("http://wnacg.com:8080/photos-index-aid-1.html", BookHost.Wnacg, GalleryLinkKind.Gallery, "1");
Expect("https://exhentai.org/s/abcdef1234/123-1", BookHost.ExHentai, GalleryLinkKind.EHentaiPage, "123", "abcdef1234", 1);

Reject("https://wnacg.com/photos-slide-aid-319097.html535483720128266257");
Reject("https://wnacg.com/photos-slide-aid-319097.html.bak");
Reject("https://www.wnacg.com/albums-index-page-1.html");
Reject("https://www.wnacg.com/?ctl=photos&act=index&aid=319097");
Reject("https://www.wnacg.com/index.php?ctl=photos&act=index&aid=319097");
Reject("https://www.wnacg.com/photos-index-aid-0.html");
Reject("https://www.wnacg.com/photos-index-aid-000000000.html");
Reject("https://e-hentai.org/g/123456");
Reject("https://e-hentai.org/g/123456/abcdef123");
Reject("https://e-hentai.org/g/123456/abcdef123g");
Reject("https://e-hentai.org/g/000/abcdef1234/");
Reject("https://e-hentai.org/s/abcdef1234/123-0");
Reject("https://hitomi.la/galleries/12.html.bak");
Reject("https://hitomi.la/doujinshi/test-abc.html");
Reject("https://www.pixiv.net/artworks/1234567890");
Reject("https://www.pixiv.net/artworks/123abc");
Reject("https://www.pixiv.net/users/5/bookmarks/artworks");
Reject("https://pixiv.net.evil.com/artworks/123");
Reject("https://wnacg.com@evil.com/photos-index-aid-1.html");
Reject("https://www.wnacg.com/photos-index-aid-5B5D20.html");
Reject("wnacg.com/photos-index-aid-319097.html");
Reject("https://www.pixiv.net/users/abc");
Reject("https://www.pixiv.net/member_illust.php?illust_id=123");

ExpectCount("看這個 https://wnacg.com/photos-index-aid-319097.html 這本", 1);
ExpectCount("https://wnacg.com/photos-index-aid-1.html https://wnacg.com/photos-index-aid-2.html", 2);
ExpectCount("https://wnacg.com/photos-index-aid-1.html https://www.wnacg.com/photos-index-aid-1.html", 1);
ExpectCount("https://e-hentai.org/g/123/abcdef1234/ https://exhentai.org/g/123/abcdef1234/", 1);
ExpectCount("https://e-hentai.org/s/abcdef1234/123-1 https://e-hentai.org/s/fedcba4321/123-2", 1);
ExpectCount("https://e-hentai.org/g/123/abcdef1234/ https://e-hentai.org/s/abcdef1234/123-2", 1);
ExpectCount("https://wnacg.com/photos-index-aid-1.html，https://wnacg.com/photos-index-aid-2.html", 2);
ExpectCount("https://hitomi.la/doujinshi/日本語-1984337.html，https://wnacg.com/photos-index-aid-1.html", 2);
ExpectCount("https://hitomi.la/doujinshi/日本語-1984337.html。", 1);
ExpectCount("**https://wnacg.com/photos-index-aid-319097.html**", 1);
ExpectCount("__https://wnacg.com/photos-index-aid-319097.html__", 1);
ExpectCount("_https://wnacg.com/photos-index-aid-319097.html_", 1);
ExpectCount("~~https://wnacg.com/photos-index-aid-319097.html~~", 1);
ExpectCount("https://wnacg.com/photos-index-aid-1.html,https://wnacg.com/photos-index-aid-2.html", 2);
ExpectCount("https://wnacg.com/photos-index-aid-1.html;https://wnacg.com/photos-index-aid-2.html", 2);
ExpectCount("https://wnacg.com/photos-index-aid-319097.html&", 1);
ExpectCount("<https://wnacg.com/photos-index-aid-319097.html>", 0);
ExpectCount("||https://wnacg.com/photos-index-aid-319097.html||", 0);
ExpectCount("|| https://wnacg.com/photos-index-aid-319097.html ||", 0);
ExpectCount("`https://wnacg.com/photos-index-aid-319097.html`", 0);
ExpectCount("#https://wnacg.com/photos-index-aid-319097.html", 0);
ExpectCount("# https://wnacg.com/photos-index-aid-319097.html", 1);
ExpectCount("[本](https://wnacg.com/photos-index-aid-319097.html)", 1);
ExpectCount("https://wnacg.com/photos-index-aid-319097.html,", 1);
ExpectCount("https://wnacg.com/photos-index-aid-319097.html這本", 1);
ExpectCount("https://wnacg.com/photos-slide-aid-319097.html535483720128266257", 0);

ExpectGuess("1496326/aa30f4bfae", BookHost.ExHentai);
ExpectGuess("1496326", BookHost.Hitomi);
ExpectGuess("319097", BookHost.Wnacg);
ExpectGuess("52600", BookHost.Wnacg);
ExpectGuess("95741451", BookHost.Pixiv);
ExpectGuess("123456789", BookHost.Pixiv);
ExpectGuess("1234567", BookHost.Hitomi);
ExpectGuess("12a45", BookHost.None);
ExpectGuess("abcdefghij", BookHost.None);
ExpectGuess("１２３４５", BookHost.None);

ExpectGodSay(BookHost.Wnacg, "319097", true);
ExpectGodSay(BookHost.ExHentai, "1496326/aa30f4bfae", true);
ExpectGodSay(BookHost.Hitomi, "1234567", true);
ExpectGodSay(BookHost.Pixiv, "123456789", true);
ExpectGodSay(BookHost.ExHentai, "1496326", false);
ExpectGodSay(BookHost.Pixiv, "1/../../users/5", false);
ExpectGodSay(BookHost.Wnacg, "1.html/../photos-view-id-9", false);
ExpectGodSay(BookHost.ExHentai, @"123\abcdef1234", false);

Console.WriteLine(failed == 0 ? "PASS" : $"FAILED ({failed})");
return failed == 0 ? 0 : 1;
