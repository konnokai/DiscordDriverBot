#:project ../DiscordDriverBot/DiscordDriverBot.csproj
#:property PublishAot=false
#nullable disable

using DiscordDriverBot.Gallery;
using DiscordDriverBot.HttpClients.NHentai;
using System.Net.Http;

// 會打真的 nhentai API，沒網路就會失敗
int failed = 0;

void Check(string name, bool ok)
{
    if (ok) return;

    Console.WriteLine($"FAIL {name}");
    failed++;
}

NHentaiAPIClient client = new();

Gallery gallery = await client.GetGalleryAsync("1234");
Check("gallery id", gallery?.Id == 1234);
Check("english title", !string.IsNullOrWhiteSpace(gallery?.Title?.English));
Check("japanese title", !string.IsNullOrWhiteSpace(gallery?.Title?.Japanese));
Check("tags", gallery?.Tags?.Count > 5);
Check("thumbnail path", !string.IsNullOrWhiteSpace(gallery?.Thumbnail?.Path));

using HttpClient http = new();
Check("thumbnail reachable", (await http.GetAsync($"https://t3.nhentai.net/{gallery.Thumbnail.Path}")).IsSuccessStatusCode);

Check("missing gallery returns null", await client.GetGalleryAsync("999999999") == null);

GallerySearch search = await client.SearchAsync("language:chinese");
Check("search results", search?.Result?.Count > 0);
Check("chinese tag id", search.Result[0].TagIds.Contains(29963));

string bookName = search.Result[0].EnglishTitle.FormatBookName();
Check("cross site search", SearchSingle.SearchNHentai(bookName, out string url, out string language)
    && url.StartsWith("https://nhentai.net/g/")
    && (language == "中文" || language == "日文"));

SearchResult multi = await SearchMulti.SearchNHentaiAsync("language:chinese", 1);
Check("search result", multi?.BookData?.Count == 25);
Check("search result language", multi.BookData[0].Language == "中文");
Check("search result url", multi.BookData[0].URL.StartsWith("https://nhentai.net/g/"));
Check("search result count", multi.SearchCount > 0);

Console.WriteLine(failed == 0 ? "PASS" : $"FAILED ({failed})");
return failed == 0 ? 0 : 1;
