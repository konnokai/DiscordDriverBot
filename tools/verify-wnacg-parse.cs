#:project ../DiscordDriverBot/DiscordDriverBot.csproj
#:property PublishAot=false
#nullable disable

using DiscordDriverBot.Gallery.Host;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

int failed = 0;

void Check(string name, bool ok)
{
    if (ok) return;

    Console.WriteLine($"FAIL {name}");
    failed++;
}

(string Title, string ThumbnailUrl, string Description, Dictionary<string, List<string>> Tags) Parse(string html)
{
    HtmlDocument document = new HtmlDocument();
    document.LoadHtml(html);

    return Wnacg.ParseIndexPage(document.DocumentNode.Descendants());
}

// 401966 的頁面：div.uwconn 裡有個站方自己用的空 <p id="rm-state">，
// InnerText 沒冒號，Split 之後只有一段，以前會在 temp[1] 炸掉 IndexOutOfRange
string page = """
<html><body>
<div id="bodywrap" class="userwrap">
    <h2>測試標題 &amp; 副標</h2>
    <div class="asTB">
        <div class="asTBcell uwthumb">
            <img alt="預覽" src="////t4.qy0.ru/data/t/4019/66/x.webp" width="240" height="302">
        </div>
        <div class="asTBcell uwconn">
            <label>分類：同人誌／漢化</label><label>頁數：30P</label>
            <label title="沒冒號的 label 不是欄位">純文字</label>
            <div class="addtags">標籤：<a class="tagshow" href="#">サインこす</a><a class="tagshow" href="#">蘿莉</a></div>
            <p id="rm-state" style="display:none;"></p>
            <p style="overflow-wrap:anywhere;">簡介：</p>
        </div>
    </div>
</div>
</body></html>
""";

var empty = Parse(page);
Check("title", empty.Title == "測試標題 & 副標");
Check("thumbnail", empty.ThumbnailUrl == "https://t4.qy0.ru/data/t/4019/66/x.webp");
Check("empty description", empty.Description == "這個上傳者很懶，甚麼都沒打");
Check("category", empty.Tags["分類:"].Single() == "同人誌／漢化");
Check("page count", empty.Tags["頁數:"].Single() == "30P");
Check("no colon label skipped", !empty.Tags.ContainsKey("純文字:"));
Check("tags", string.Join(", ", empty.Tags["標籤:"]) == "サインこす, 蘿莉");

// 只有空 <p> 沒有簡介欄位時，以前直接爆掉，現在當成沒填簡介
var noDescription = Parse(page.Replace("<p style=\"overflow-wrap:anywhere;\">簡介：</p>", ""));
Check("empty p skipped", noDescription.Description == "");

// 簡介裡的冒號要留在內容裡，不能被切掉
var withText = Parse(page.Replace("簡介：", "簡介：有內容：還有冒號"));
Check("description keeps rest", withText.Description == "有內容：還有冒號");

Console.WriteLine(failed == 0 ? "PASS" : $"FAILED ({failed})");
return failed == 0 ? 0 : 1;
