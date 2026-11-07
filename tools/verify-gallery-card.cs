#:project ../DiscordDriverBot/DiscordDriverBot.csproj
#:property PublishAot=false
#nullable disable

using Discord;
using DiscordDriverBot.Gallery;
using DiscordDriverBot.Gallery.Host.Pixiv;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

int failed = 0;

void Check(string name, bool ok)
{
    if (ok) return;

    Console.WriteLine($"FAIL {name}");
    failed++;
}

IEnumerable<IMessageComponent> Flatten(IEnumerable<IMessageComponent> components)
{
    foreach (IMessageComponent component in components)
    {
        yield return component;

        IEnumerable<IMessageComponent> children = component switch
        {
            ContainerComponent container => container.Components,
            SectionComponent section => section.Components,
            ActionRowComponent actionRow => actionRow.Components,
            _ => null
        };

        if (children == null) continue;

        foreach (IMessageComponent child in Flatten(children)) yield return child;
    }
}

// profile/top 的回應：最新插畫要拿到縮圖和分級
PixivWork work = JsonConvert.DeserializeObject<PixivWork>("""
{ "id": "151654634", "title": "[C99] 長離", "url": "https://i.pximg.net/c/250x250_80_a2/img.jpg", "xRestrict": 2, "createDate": "2026-11-04T00:06:45+09:00" }
""");
Check("work url", work.Url == "https://i.pximg.net/c/250x250_80_a2/img.jpg");
Check("work xRestrict", work.XRestrict == 2);
Check("work title", work.Title == "[C99] 長離");

// pixiv.cat：多圖要拿到每一頁的原圖代理，不能只拿第一張縮圖
PixivCat multi = JsonConvert.DeserializeObject<PixivCat>("""
{ "success": true, "multiple": true, "artist": { "id": 1, "name": "かぼみーる" }, "original_urls_proxy": ["https://i.pixiv.cat/a.png", "https://i.pixiv.cat/b.png"] }
""");
Check("pixiv cat images", multi.OriginalUrlsProxy.Count == 2);
Check("pixiv cat artist", multi.Artist?.Name == "かぼみーる");

// ajax/user 的回應：個人頁橫幅
PixivUserResponse user = JsonConvert.DeserializeObject<PixivUserResponse>("""
{ "error": false, "body": { "name": "時雨しずく", "comment": "繪畫中", "image": "https://i.pximg.net/a.png", "imageBig": "https://i.pximg.net/b.png", "webpage": "https://x.com/x", "background": { "repeat": null, "color": null, "url": "https://i.pximg.net/c/1920x960/bg.jpg", "isPrivate": false } } }
""");
Check("user background", user.Body.Background?.Url == "https://i.pximg.net/c/1920x960/bg.jpg");
Check("user body", user.Body.Name == "時雨しずく");

GalleryCard card = GalleryMessage.Card()
    .WithHeader("[C99] 長離", "https://www.pixiv.net/artworks/151654634", "https://i.pximg.net/thumb.jpg")
    .WithField("標籤", "長離, 鳴潮")
    .WithField("其他網站", "   ")
    .WithMediaGallery(new MediaGalleryBuilder().AddItem("https://i.pximg.net/c/250x250_80_a2/img.jpg", "單圖"));

ContainerComponent root = card.Build().Components.Single() as ContainerComponent;

Check("root container", root != null);
Check("accent color", root.AccentColor == new Color(0x00, 0xE5, 0x84));
Check("child count", root.Components.Count == 3);

if (root.Components.Count == 3)
{
    SectionComponent header = root.Components.First() as SectionComponent;
    Check("header section", header != null);
    Check("header text", (header.Components.First() as TextDisplayComponent)?.Content == "[［C99］ 長離](https://www.pixiv.net/artworks/151654634)");
    Check("header thumbnail", (header.Accessory as ThumbnailComponent)?.Media.Url == "https://i.pximg.net/thumb.jpg");

    Check("field text", (root.Components.ElementAt(1) as TextDisplayComponent)?.Content == "**標籤**\n長離, 鳴潮");
    Check("gallery item", (root.Components.ElementAt(2) as MediaGalleryComponent)?.Items.Single().Media.Url == "https://i.pximg.net/c/250x250_80_a2/img.jpg");
}

// 標題裡的 markdown 連結語法會壞掉，要換成全形括號
Check("escape link text", "[a]\n[b]".EscapeLinkText() == "［a］ ［b］");

// 沒有縮圖時不能產生 Section，不然 Discord 會退件
GalleryCard plain = GalleryMessage.Card().WithHeader("沒有縮圖", "https://example.com/1");
ContainerComponent plainRoot = plain.Build().Components.Single() as ContainerComponent;
Check("header without thumbnail", (plainRoot.Components.Single() as TextDisplayComponent)?.Content == "[沒有縮圖](https://example.com/1)");

// 作者放在標題上方，footer 留給觸發者
GalleryCard byline = GalleryMessage.Card().WithHeader("標題", null, null, "かぼみーる");
Check("creator byline", (byline.Build().Components.Single() as ContainerComponent).Components
    .Cast<TextDisplayComponent>().Single().Content == "-# かぼみーる\n標題");

// 連結文字是網址時 Discord 會把整串 markdown 原文照印，Text() 要換成網域
GalleryCard urlCard = GalleryMessage.Card()
    .Text("差分8枚 + α\n[https://kabomeel.fanbox.cc/posts/12785485](https://kabomeel.fanbox.cc/posts/12785485)")
    .Text("[https://fantia.jp/posts/747737](/jump.php?https%3A%2F%2Ffantia.jp%2Fposts%2F747737)")
    .Text("[完整版](https://example.com/2)");
string urlText = (urlCard.Build().Components.Single() as ContainerComponent).Components
    .Cast<TextDisplayComponent>().First().Content;
Check("url label", urlText == "差分8枚 + α\n[kabomeel.fanbox.cc](https://kabomeel.fanbox.cc/posts/12785485)");
string[] otherText = (urlCard.Build().Components.Single() as ContainerComponent).Components
    .Cast<TextDisplayComponent>().Skip(1).Select((x) => x.Content).ToArray();
Check("jump link", otherText[0] == "[fantia.jp](https://fantia.jp/posts/747737)");
Check("normal mask", otherText[1] == "[完整版](https://example.com/2)");


// 所有文字加起來不能超過 CV2 的 4000 字上限
GalleryCard longCard = GalleryMessage.Card()
    .WithHeader("很長的標題", "https://example.com/2")
    .WithField("標籤", new string('長', 5000))
    .Text(new string('畫', 5000));
int total = Flatten(longCard.Build().Components).OfType<TextDisplayComponent>().Sum((x) => x.Content.Length);
Check("text budget", total <= GalleryCard.MaxTextLength);

// pixiv 的內文是 HTML，轉完 markdown 還會留下 span，分隔線也會吃掉字數
Check("clean span", "<span style=\"color:#d47eff;\">MOD</span>".CleanText() == "MOD");
Check("clean separator", "A\n----------------\nB".CleanText() == "A\n\nB");
Check("clean keep angle text", "<Fate/stay night> 很好看".CleanText() == "<Fate/stay night> 很好看");

// 內文只留開頭，切在連結中間時不能留下半截 markdown
string head = new string('字', GalleryCard.DescriptionLength - 10);
GalleryCard clipped = GalleryMessage.Card().Text(head + "[x](https://example.com/verylong)", GalleryCard.DescriptionLength);
string clippedText = (clipped.Build().Components.Single() as ContainerComponent).Components
    .Cast<TextDisplayComponent>().Single().Content;
Check("description limit", clippedText.Length <= GalleryCard.DescriptionLength);
Check("description link cut", clippedText == head + "…");

// 純文字也不能超過上限
GalleryCard plainClip = GalleryMessage.Card().Text(new string('字', 300), GalleryCard.DescriptionLength);
string plainClipText = (plainClip.Build().Components.Single() as ContainerComponent).Components
    .Cast<TextDisplayComponent>().Single().Content;
Check("description plain limit", plainClipText.Length <= GalleryCard.DescriptionLength && plainClipText.EndsWith("…"));

// 換頁列放在 Container 外面，跟參考卡片的按鈕一樣在下方
GalleryCard paged = GalleryMessage.Card()
    .WithHeader("標題", null)
    .WithActionRow(GalleryCard.PagerRow("pixiv-page", "abc", 2, 5));
Check("pager row outside container", paged.Build().Components.Count == 2 && paged.Build().Components.ElementAt(1) is ActionRowComponent);

ActionRowComponent pager = GalleryCard.PagerRow("pixiv-page", "abc", 2, 5).Build();
Check("pager buttons", pager.Components.Count == 5);
Check("pager first id", (pager.Components.First() as ButtonComponent)?.CustomId == "pixiv-page:abc:first:1");
Check("pager first enabled", !(pager.Components.First() as ButtonComponent).IsDisabled);
Check("pager indicator", (pager.Components.ElementAt(2) as ButtonComponent)?.Label == "2/5" && (pager.Components.ElementAt(2) as ButtonComponent).IsDisabled);
Check("pager last id", (pager.Components.ElementAt(4) as ButtonComponent)?.CustomId == "pixiv-page:abc:last:5");
Check("pager unique ids", pager.Components.Cast<ButtonComponent>().Select((x) => x.CustomId).Distinct().Count() == 5);

ActionRowComponent pagerEdge = GalleryCard.PagerRow("pixiv-page", "abc", 1, 5).Build();
Check("pager back disabled", (pagerEdge.Components.First() as ButtonComponent).IsDisabled);

// 上傳時間要保留 pixiv 的 +09:00，不能換算成本機時區
IllustMetadata dated = JsonConvert.DeserializeObject<IllustMetadata>("""{ "body": { "createDate": "2026-11-04T00:06:45+09:00" } }""");
Check("upload date", dated.Body.CreateDate.ToString("yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) == "2026/11/04 00:06");

Console.WriteLine(failed == 0 ? "PASS" : $"FAILED ({failed})");
return failed == 0 ? 0 : 1;
