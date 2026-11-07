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
            string title, description, creator;
            List<string> tags;
            List<string> imageUrls;

            if (SQLite.SQLiteFunction.GetBookData($"https://www.pixiv.net/artworks/{id}", out SQLite.Table.BookData bookData))
            {
                title = bookData.Title;
                description = bookData.ExtensionData;
                tags = JsonConvert.DeserializeObject<List<string>>(bookData.Tags.Trim('"').Replace("\\", string.Empty));

                // 舊快取的縮圖是 250x250 方形裁切，重抓頁面原圖才能放大顯示
                var cachedImages = await GetImageUrlsFromPixivCatAsync(id.ToString());
                creator = cachedImages.ArtistName;
                imageUrls = cachedImages.Status ? ToProxyUrls(cachedImages.ImageUrls) : new();

                if (imageUrls.Count == 0 && !string.IsNullOrWhiteSpace(bookData.ThumbnailUrl))
                    imageUrls.Add(bookData.ThumbnailUrl);
            }
            else
            {
                try
                {

                    var result = await GetIllustDataFromAjaxAsync(id.ToString());
                    if (!result.Status)
                        throw new Exception(string.IsNullOrEmpty(result.Error) ? "找不到資料" : result.Error);

                    var imageUrlResult = await GetImageUrlsFromPixivCatAsync(id.ToString());
                    if (!imageUrlResult.Status)
                        throw new Exception(string.IsNullOrEmpty(imageUrlResult.Error) ? "找不到資料" : imageUrlResult.Error);

                    var converter = new Converter();
                    Body illust = result.Reslut.Body;
                    title = illust.Title;
                    creator = imageUrlResult.ArtistName;
                    description = converter.Convert(illust.Description);
                    imageUrls = ToProxyUrls(imageUrlResult.ImageUrls);
                    tags = illust.Tags.Tags.Select((x) => x.Tag).ToList();

                    new SQLite.Table.BookData($"https://www.pixiv.net/artworks/{id}", title, description, imageUrls.FirstOrDefault(), tags).InsertNewData();
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

            Log.New($"{imageUrls.FirstOrDefault()}");

            // 作品圖放大顯示；R-18 在一般頻道只留 Pixiv logo 當提示
            bool showImages = guild.Id != 463657254105645056;
            string headerThumbnail = null;
            if (showImages)
            {
                bool isR18 = tags.Contains("R-18") || tags.Contains("R-18G");
                if (isR18 && !((ITextChannel)messageChannel).IsNsfw)
                {
                    headerThumbnail = "https://s.pximg.net/www/images/pixiv_logo.gif";
                    showImages = false;
                }
            }

            GalleryCard card = GalleryMessage.Card()
                .WithHeader(title, $"https://www.pixiv.net/artworks/{id}", headerThumbnail, creator);

            if (!string.IsNullOrWhiteSpace(description))
                card.Text(description);

            card.WithField("標籤", string.Join(", ", tags));

            if (bookData != null) card.WithField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過");

            if (showImages)
            {
                // 每張各自一個 MediaGallery 才會上下排、維持原比例；只放前兩張，其餘回原網站看
                foreach (string imageUrl in imageUrls.Take(2))
                    card.WithMediaGallery(new MediaGalleryBuilder().AddItem(imageUrl));
            }

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

        private static async Task<(bool Status, List<string> ImageUrls, string ArtistName, string Error)> GetImageUrlsFromPixivCatAsync(string id)
        {
            var content = new FormUrlEncodedContent([new("p", id)]);

            try
            {
                var postResult = await _httpClient.PostAsync("https://api.pixiv.cat/v1/generate", content);
                postResult.EnsureSuccessStatusCode();

                PixivCat pixivCat = JsonConvert.DeserializeObject<PixivCat>(await postResult.Content.ReadAsStringAsync());

                if (pixivCat == null)
                {
                    return (false, null, null, "");
                }
                else if (!pixivCat.Success)
                {
                    Log.Error(pixivCat.Error);
                    return (false, null, null, pixivCat.Error);
                }

                // 多圖才有 original_urls_proxy，單圖只有 original_url_proxy
                List<string> urls = pixivCat.OriginalUrlsProxy;
                if (urls == null || urls.Count == 0)
                    urls = string.IsNullOrWhiteSpace(pixivCat.OriginalUrlProxy) ? new() : new() { pixivCat.OriginalUrlProxy };

                return (true, urls, pixivCat.Artist?.Name, "");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, null, null, "找不到此Id的資料，可能已被刪除");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return (false, null, null, "發生了未知的錯誤");
            }
        }

        private static List<string> ToProxyUrls(List<string> urls) =>
            urls.Select(GetProxyImageUrl).Where((x) => x != null).ToList();

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