#:project ../DiscordDriverBot/DiscordDriverBot.csproj
#:property PublishAot=false
#nullable disable

using DiscordDriverBot.Interaction.Gallery;

int failed = 0;

void Expect(string input, params string[] expected)
{
    string result = Gallery.BuildFileNameResult(input);
    if (result == null)
    {
        Console.WriteLine($"FAIL: {input} -> no result");
        failed++;
        return;
    }

    foreach (string part in expected)
    {
        if (result.Contains(part)) continue;
        Console.WriteLine($"FAIL: {input} -> {result.Replace("\n", " | ")}, expected {part}");
        failed++;
    }
}

void Reject(string input)
{
    string result = Gallery.BuildFileNameResult(input);
    if (result == null) return;

    Console.WriteLine($"FAIL reject: {input} -> {result.Replace("\n", " | ")}");
    failed++;
}

// Twitter Media Downloader 的 {帳號}-{推文 ID}-{序號}
Expect("otaku_eden2014-2118208653950140848-01.jpg",
    "https://x.com/otaku_eden2014",
    "https://x.com/otaku_eden2014/status/2118208653950140848");
Expect("otaku_eden2014-2118208653950140848.jpg", "https://x.com/otaku_eden2014");
Expect("https://pbs.twimg.com/media/otaku_eden2014-2118208653950140848-01.jpg", "https://x.com/otaku_eden2014");

// Pixiv 的 {作品 ID}_p{頁碼}[_{尺寸}]
Expect("151718411_p2_master1200.jpg", "https://www.pixiv.net/artworks/151718411");
Expect("151718411_p0.jpg", "https://www.pixiv.net/artworks/151718411");
Expect("https://i.pximg.net/img-original/img/2026/01/01/00/00/00/151718411_p2_master1200.jpg", "https://www.pixiv.net/artworks/151718411");

// X 圖片 ID
Expect("HWVjjErbQbA.jpg", "上傳時間：2026-11-05 05:07:52 UTC");
Expect("HWVjjErbQbA:orig", "HWVjjErbQbA");
Expect("https://pbs.twimg.com/media/HWVjjErbQbA?format=jpg&name=small", "HWVjjErbQbA");

Reject("");
Reject("not an image");
Reject("151718411.jpg");
Reject("151718411_px_master1200.jpg");
Reject("otaku_eden2014-2118208653950140848-x1.jpg");
Reject("HWVjjErbQb");

Console.WriteLine(failed == 0 ? "OK" : $"{failed} failed");
return failed == 0 ? 0 : 1;
