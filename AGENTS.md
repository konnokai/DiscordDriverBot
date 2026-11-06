# AGENTS.md

已上線的 Discord Bot，有真實使用者。框架為 .NET 10 與 Discord.Net。

## 結構

- `DiscordDriverBot/Interaction/`：slash 與 message command（Discord.Net.Interactions）。
- `DiscordDriverBot/Command/`：`!!` 前綴的文字指令（Discord.Commands），前綴定義在 `CommandHandler.cs`。
- `DiscordDriverBot/HttpClients/`：對外 API 與爬頁客戶端。新程式碼從注入的 `IHttpClientFactory` 取 HttpClient；部分既有靜態客戶端自行建立。
- `DiscordDriverBot/Gallery/`：各網站的資料解析與卡片顯示。
- `Data/`：Docker 透過 volume 掛到 `/app/Data`。`Program.GetDataFilePath()` 用的是 `AppDomain.BaseDirectory/Data`，本機執行期資料在 `DiscordDriverBot/bin/<組態>/net10.0/Data/`。`HelpDescription.txt` 在根 `Data/` 與 `DiscordDriverBot/Data/` 各一份，改動要同步。`Data/*.db` 不進版控。
- `Dockerfile` 用官方 `sdk:10.0-noble`／`runtime:10.0-noble` 自建 image 並裝 opencc（`Program.cs` 會呼叫 `opencc -c s2tw`）。.NET 10 沒有 Debian bookworm tag，別改回 `bookworm-slim`。`dotnet_with_opencc/` 是獨立的上游 image 原始碼，不參與主 image 建置。

## 指令註冊

`InteractionHandler.CommandSignature` 把所有指令的註冊規格（群組路徑、名稱、描述、權限與 ContextTypes／IntegrationTypes／NSFW、參數順序與型別／必填／範圍／choices、context 指令）排序後算 SHA256。Release 啟動時比對 `Data/CommandSignature.txt`，不同就重新註冊全域指令並寫回簽章；改名稱、描述、參數或 enum 選項都會觸發。簽章含 guild 專用模組，不含不註冊到 Discord 的 component 指令。刪掉該檔可強制重新註冊。

DEBUG 啟動時用 `^[\w-]{1,32}$` 檢查 slash command 與參數名稱：名稱不符只寫 log，參數不符會讓 `InitializeAsync` 提早 return，指令事件就不會掛上。名稱慣例用英文小寫加連字號。

## 設定

`Utility.InDocker` 判斷 `DOTNET_RUNNING_IN_CONTAINER == "true"`：容器內由 `docker-compose.yml` 的 `env_file` 把 `.env`（範本 `.env_sample`）注入成環境變數，`BotConfig` 讀環境變數；本機讀工作目錄的 `bot_config.json` 並自動產生 `bot_config_example.json`。必要欄位缺失會直接結束。新增設定欄位時，同步更新 `BotConfig.cs`、`.env_sample` 與 `README.md`。

## 驗證

`dotnet build DiscordDriverBot.sln -c Debug`。專案沒有測試專案；`tools/verify-*.cs` 用 `#:project` 參照主專案，執行方式是 `dotnet run tools/<檔名>.cs`（file-based app，需要 .NET 10 SDK）。改到新的解析或編解碼邏輯時，沿用這個方式加 verify 檔，或在系統暫存目錄開 self-check console 專案跑 assert。

## 已知坑

- 貼網址自動抓資料與 `!!` 文字指令都需要 `MessageContentIntent=true`；`.env_sample` 預設是 `false`。沒開 intent 時改用右鍵指令。
- Ascii2D 走 FlareSolverr。ascii2d.net 回中文 Cloudflare 驗證頁，FlareSolverr 認不出來，`Ascii2DClient.FindAsync` 自己 POST `/v1` 帶 `waitInSeconds` 等 JS 驗證跑完（秒數寫在該方法裡）。`FindAsync` 是 async iterator，每次列舉都會重發請求，呼叫端只能列舉一次。
- `Command/Normal/Normal.cs` 的 `SauceAsync` 同時跑 Ascii2D 與 SauceNAO，SauceNAO 先啟動再 await，避免被等待拖慢。
- `HttpClients/Ascii2D/Result.cs` 與 `HttpClients/SauceNAO/Result.cs` 都叫 `Result`，兩個命名空間同時 using 時要寫完整名稱。
