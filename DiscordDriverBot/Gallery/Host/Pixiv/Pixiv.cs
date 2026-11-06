using Discord;
using DiscordDriverBot.Command;
using Html2Markdown;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordDriverBot.Gallery.Host.Pixiv
{
    static class Pixiv
    {
        static readonly HttpClient _httpClient = new();

        public static async Task<bool> GetDataAsync(GalleryLink link, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            if (link.Kind != GalleryLinkKind.Gallery || !int.TryParse(link.Id, out int id))
            {
                Log.Error($"Pixiv Parse Error: {link.SourceUrl}");
                return false;
            }

            return await GetIllustData(id, guild, messageChannel, user, interactionContext);
        }

        public static async Task<bool> GetUserDataAsync(GalleryLink link, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            if (link.Kind != GalleryLinkKind.PixivUser) return false;

            string id = link.Id;

            try
            {
                PixivUserResponse userData = JsonConvert.DeserializeObject<PixivUserResponse>(
                    await _httpClient.GetStringAsync($"https://www.pixiv.net/ajax/user/{id}?full=1&lang=zh_tw"));

                if (userData?.Body == null || userData.Error)
                {
                    await SendErrorAsync(messageChannel, interactionContext, $"{user.Mention} 找不到 ID {id} 的使用者");
                    return false;
                }

                // profile/top 的 pickup 未登入時拿不到，改用依發布時間排序的最新三張
                List<PixivWork> works = new();
                try
                {
                    PixivUserTopResponse topData = JsonConvert.DeserializeObject<PixivUserTopResponse>(
                        await _httpClient.GetStringAsync($"https://www.pixiv.net/ajax/user/{id}/profile/top?lang=zh_tw"));

                    var illusts = topData?.Body?.Illusts as JObject;
                    if (illusts != null)
                        works = illusts.Properties()
                            .Select((x) => x.Value.ToObject<PixivWork>())
                            .OrderByDescending((x) => x.CreateDate)
                            .Take(3)
                            .ToList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "取得 Pixiv 使用者插畫失敗");
                }

                GalleryCard card = GalleryMessage.Card();

                // 個人頁橫幅就是 pixiv 網站版最上面那張，Discord 自己的連結預覽也是抓這張
                string banner = GetProxyImageUrl(userData.Body.Background?.Url);
                if (banner != null && guild.Id != 463657254105645056)
                    card.WithMediaGallery(new MediaGalleryBuilder().AddItem(banner));

                string avatar = GetProxyImageUrl(userData.Body.ImageBig) ?? GetProxyImageUrl(userData.Body.Image);
                card.WithHeader(userData.Body.Name, $"https://www.pixiv.net/users/{id}", guild.Id == 463657254105645056 ? null : avatar);

                if (!string.IsNullOrWhiteSpace(userData.Body.Comment))
                    card.Text(userData.Body.Comment);

                card.WithField("其他網站", userData.Body.Webpage);

                if (works.Count > 0)
                {
                    card.Text("**最新插畫**");

                    // 這個伺服器不顯示任何圖，其他頻道則是 NSFW 才給 R-18 縮圖
                    if (guild.Id != 463657254105645056)
                    {
                        bool isNsfw = ((ITextChannel)messageChannel).IsNsfw;
                        MediaGalleryBuilder gallery = new MediaGalleryBuilder();
                        foreach (PixivWork work in works.Where((x) => isNsfw || x.XRestrict == 0))
                        {
                            string thumbnail = GetProxyImageUrl(work.Url);
                            if (thumbnail != null) gallery.AddItem(thumbnail, work.Title);
                        }
                        card.WithMediaGallery(gallery);
                    }

                    card.Text(string.Join("\n",
                        works.Select((x) => $"[{x.Title.EscapeLinkText()}](https://www.pixiv.net/artworks/{x.Id})")));
                }

                card.WithFooter(user);
                await card.SendAsync(messageChannel, interactionContext);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "取得 Pixiv 使用者資料失敗");
                await SendErrorAsync(messageChannel, interactionContext, $"{user.Mention} 找不到此使用者或 Pixiv 暫時無法使用");
                return false;
            }
        }

        private static async Task<bool> GetIllustData(int id, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            string thumbnailURL, title, description;
            List<string> tags;

            if (SQLite.SQLiteFunction.GetBookData($"https://www.pixiv.net/artworks/{id}", out SQLite.Table.BookData bookData))
            {
                title = bookData.Title;
                description = bookData.ExtensionData;
                thumbnailURL = bookData.ThumbnailUrl;
                tags = JsonConvert.DeserializeObject<List<string>>(bookData.Tags.Trim('"').Replace("\\", string.Empty));
            }
            else
            {
                try
                {

                    var result = await GetIllustDataFromAjaxAsync(id.ToString());
                    if (!result.Status)
                        throw new Exception(string.IsNullOrEmpty(result.Error) ? "找不到資料" : result.Error);

                    var thumbnailUrlResult = await GetThumbnailUrlFromPixivCatAsync(id.ToString());
                    if (!thumbnailUrlResult.Status)
                        throw new Exception(string.IsNullOrEmpty(thumbnailUrlResult.Error) ? "找不到資料" : thumbnailUrlResult.Error);

                    var converter = new Converter();
                    Body illust = result.Reslut.Body;
                    title = illust.Title;
                    description = illust.Description;
                    description = converter.Convert(illust.Description);
                    thumbnailURL = GetProxyImageUrl(thumbnailUrlResult.ThumbnailUrl);
                    tags = illust.Tags.Tags.Select((x) => x.Tag).ToList();

                    new SQLite.Table.BookData($"https://www.pixiv.net/artworks/{id}", title, description, thumbnailURL, tags).InsertNewData();
                }
                catch (Exception ex)
                {
                    if (interactionContext == null)
                        await messageChannel.SendErrorAsync("發生了未知的錯誤");
                    else
                        await interactionContext.Interaction.FollowupAsync("發生了未知的錯誤", ephemeral: true);
                    Log.Error(ex.ToString());
                    return false;
                }
            }

            Log.New($"{thumbnailURL}");

            string headerThumbnail = null;
            if (guild.Id != 463657254105645056)
            {
                bool isR18 = tags.Contains("R-18") || tags.Contains("R-18G");
                headerThumbnail = isR18 && !((ITextChannel)messageChannel).IsNsfw
                    ? "https://s.pximg.net/www/images/pixiv_logo.gif"
                    : thumbnailURL;
            }

            GalleryCard card = GalleryMessage.Card()
                .WithHeader(title, string.Format("https://www.pixiv.net/artworks/{0}", id), headerThumbnail);

            if (!string.IsNullOrWhiteSpace(description))
                card.Text(description);

            card.WithField("標籤", string.Join(", ", tags));

            if (bookData != null) card.WithField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過");
            card.WithFooter(user);

            try
            {
                await card.SendAsync(messageChannel, interactionContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "發送 Pixiv 訊息失敗");
#if RELEASE
                if (ex.Message.Contains("50013"))
                    await user.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription($"你在 {guild.Name}/{messageChannel.Name} 使用到了Bot的功能，但Bot無讀取&發言&嵌入連結權限\n請向管理員要求提供Bot權限")
                        .Build());
#endif
                return false;
            }

            return true;
        }

        private static async Task<(bool Status, IllustMetadata Reslut, string Error)> GetIllustDataFromAjaxAsync(string id)
        {
            try
            {
                string result = await _httpClient.GetStringAsync($"https://www.pixiv.net/ajax/illust/{id}");
                var illust = JsonConvert.DeserializeObject<IllustMetadata>(result);

                if (illust == null)
                {
                    return (false, null, "");
                }
                else if (illust.Error && !string.IsNullOrWhiteSpace(illust.Message))
                {
                    Log.Error(illust.Message);
                    return (false, null, illust.Message);
                }

                return (true, illust, "");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, null, "找不到此Id的資料，可能已被刪除");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return (false, null, "發生了未知的錯誤");
            }
        }

        private static async Task<(bool Status, string ThumbnailUrl, string Error)> GetThumbnailUrlFromPixivCatAsync(string id)
        {
            var content = new FormUrlEncodedContent([new("p", id)]);

            try
            {
                var postResult = await _httpClient.PostAsync("https://api.pixiv.cat/v1/generate", content);
                postResult.EnsureSuccessStatusCode();

                PixivCat pixivCat = JsonConvert.DeserializeObject<PixivCat>(await postResult.Content.ReadAsStringAsync());

                if (pixivCat == null)
                {
                    return (false, null, "");
                }
                else if (!pixivCat.Success)
                {
                    Log.Error(pixivCat.Error);
                    return (false, null, pixivCat.Error);
                }

                // Thumbnails欄位只有在多圖的時候會出現
                // 感覺直接回傳OriginalUrl不太好 :thinking:
                return (true, pixivCat.Thumbnails != null ? pixivCat.Thumbnails.First() : pixivCat.OriginalUrlProxy, "");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, null, "找不到此Id的資料，可能已被刪除");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return (false, null, "發生了未知的錯誤");
            }
        }

        private static string GetProxyImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return null;

            if (uri.Host.Equals("i.pximg.net", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("i.pixiv.cat", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("i-cf.pixiv.cat", StringComparison.OrdinalIgnoreCase))
                return "https://pixiv.konnokai.workers.dev" + uri.PathAndQuery;

            return url;
        }

        private static async Task SendErrorAsync(IMessageChannel messageChannel, IInteractionContext interactionContext, string message)
        {
            if (interactionContext == null)
                await messageChannel.SendErrorAsync(message);
            else
                await interactionContext.Interaction.FollowupAsync(message, ephemeral: true);
        }
    }
}