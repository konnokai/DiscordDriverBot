# 飆車小幫手

Discord Bot，用來查本本資訊、以圖搜圖，以及處理推特／X 網址。使用 .NET 10。

## 功能

- 貼上 Pixiv、ExHentai、NHentai、Hitomi、Wnacg 網址，Bot 自動抓資料並顯示卡片。Pixiv 卡片會去掉內文的 HTML、只留前 200 字，標籤可點，多圖有換頁按鈕，並顯示上傳時間。
- 以圖搜圖：對含圖片的訊息按右鍵選「搜尋 Ascii2D」／「搜尋 SauceNAO」；`/gallery sauce-from-url`、`/gallery sauce-from-attachment` 只跑 SauceNAO，`!!sauce` 兩邊都跑。
- 右鍵「反推圖片來源」：從檔名找來源。`{帳號}-{推文 ID}-{序號}` 給 X 帳號與推文、`{作品 ID}_p{頁碼}` 給 Pixiv 作品頁；純 X 圖片 ID 則換成上傳時間與 X 搜尋頁。
- 推特／X：`/convert-to-vxtwitter` 與右鍵「轉換網址成 vxTwitter」。
- 舊文字指令用 `!!` 前綴（例如 `!!godsay`、`!!help`），也可以直接 mention Bot。右鍵指令在訊息的「應用程式」選單。

## 用 Docker 跑（推薦）

1. 複製 `.env_sample` 成 `.env`，填入數值。
2. `docker compose up -d --build`

`docker-compose.yml` 會一起帶起 FlareSolverr（Ascii2D 需要），`FlareSolverrApiUrl` 用預設的 `http://flaresolverr:8191/` 就好。`Data/` 會掛進容器，資料庫留在這裡。Bot 啟動時會自動套用未執行的資料庫 migration。

`Dockerfile` 用官方 `mcr.microsoft.com/dotnet/sdk:10.0-noble` 與 `runtime:10.0-noble` 自己裝 opencc，不依賴外部 image。`dotnet_with_opencc/` 是另一個獨立 image 的原始碼（`build.sh` 會 build／push），改它不會影響這裡的建置。

## 本機跑

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。設定不讀 `.env`，改放在工作目錄的 `bot_config.json`；第一次執行會自動產生 `bot_config_example.json` 當範本。

```powershell
dotnet build DiscordDriverBot.sln -c Debug
dotnet run --project DiscordDriverBot/DiscordDriverBot.csproj
```

Visual Studio 2026（18.0 以上）可以開 `DiscordDriverBot.sln`；VS 2022 不支援 net10.0。

## 設定

必要：`DiscordToken`、`WebHookUrl`、`ExHentaiCookieMemberId`、`ExHentaiCookiePassHash`、`ExHentaiCookieSK`、`SauceNAOApiKey`、`FlareSolverrApiUrl`。少一項 Bot 會直接結束。

選填：`GitHubApiKey`、`NHentaiApiKey`、`MessageContentIntent`、`TestSlashCommandGuildId`、`UptimeKumaPushUrl`。各項說明見 `.env_sample`。

貼網址自動抓資料與 `!!` 文字指令都需要 `MessageContentIntent=true`，並在 Discord Developer Portal 開啟 Message Content Intent；`.env_sample` 預設是 `false`。沒開的話，右鍵指令仍可解析網址。
