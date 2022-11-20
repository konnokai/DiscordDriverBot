using Discord;
using Discord_Driver_Bot.Command;
using Html2Markdown;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Gallery.Host.Pixiv
{
    static class Pixiv
    {
        static HttpClient HttpClient = new HttpClient();

        static Regex regex = new Regex(@"artworks\/(?'Id'\d{0,9})");

        public static async Task GetDataAsync(string url, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            var reg = regex.Match(url);
            if (!reg.Success) return;

            if (!int.TryParse(reg.Groups["Id"].Value, out int id))
            {
                Log.Error($"Pixiv Parse Error: {url} ({reg.Groups["id"].Value})");
                return;
            }

            /*if (url.Contains("member.php") || url.Contains("users")) GetMenberData(id, e);
            else*/
            if (url.Contains("member_illust.php") || url.Contains("artworks")) await GetIllustData(id, guild, messageChannel, user, interactionContext);
        }

        private static async Task GetIllustData(int id, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
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
                var result = await GetIllustDataFromAjaxAsync(id.ToString());
                var converter = new Converter();

                if (!result.Status)
                {
                    if (string.IsNullOrEmpty(result.Error))
                        return;
                    else
                        throw new Exception(result.Error);
                }

                Body illust = result.Reslut.Body;
                title = illust.Title;
                description = illust.Description;
                description = converter.Convert(illust.Description);
                thumbnailURL = illust.Urls.Thumb.Replace("i.pximg.net", "pixiv.konnokai.workers.dev");
                tags = illust.Tags.Tags.Select((x) => x.Tag).ToList();

                new SQLite.Table.BookData($"https://www.pixiv.net/artworks/{id}", title, description, thumbnailURL, tags).InsertNewData();
            }

            Log.NewBook($"{thumbnailURL}");

            EmbedBuilder discordEmbedBuilder = new EmbedBuilder().WithOkColor()
                .WithTitle(title)
                .WithDescription(description)
                .WithUrl(string.Format("https://www.pixiv.net/artworks/{0}", id))
                .AddField("標籤", string.Join(", ", tags), true);

            if (guild.Id != 463657254105645056)
            {
                if (tags.Contains("R-18"))
                {
                    if (((ITextChannel)messageChannel).IsNsfw) discordEmbedBuilder.WithThumbnailUrl(thumbnailURL);
                    else discordEmbedBuilder.WithThumbnailUrl("https://s.pximg.net/www/images/pixiv_logo.gif");
                }
                else discordEmbedBuilder.WithThumbnailUrl(thumbnailURL);
            }

            if (bookData != null) discordEmbedBuilder.AddField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過", true);
            discordEmbedBuilder.WithFooter(user.Username + " ID: " + user.Id, user.GetAvatarUrl());

            try
            {
                if (interactionContext == null)
                    await messageChannel.SendMessageAsync(embed: discordEmbedBuilder.Build());
                else
                    await interactionContext.Interaction.FollowupAsync(embed: discordEmbedBuilder.Build());

            }
            catch (Exception ex)
            {
#if RELEASE
                if (ex.Message.Contains("50013"))
                    await user.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription($"你在 {guild.Name}/{messageChannel.Name} 使用到了Bot的功能，但Bot無讀取&發言&嵌入連結權限\n請向管理員要求提供Bot權限")
                        .Build());
#endif
            }
        }

        //private static void GetMenberData(long id, SocketMessage e)
        //{
        //    var jObject = GetPixivData($"https://api.imjad.cn/pixiv/v1/?type=member_illust&id={id}").Reslut;
        //    string title = jObject["title"].ToString();
        //    string description = jObject["caption"].ToString();
        //    string thumbnailURL = jObject["image_urls"]["px_128x128"].ToString().Replace("pximg.net", "pixiv.cat");
        //    List<string> tags = jObject["tags"].Select((x) => x.ToString()).ToList();

        //    Log.FormatColorWrite(thumbnailURL, ConsoleColor.Green);

        //    EmbedBuilder discordEmbedBuilder = new EmbedBuilder()
        //        .WithOkColor()
        //        .WithTitle($"{GetPixivData($"https://api.imjad.cn/pixiv/v1/?type=member&id={id}").Reslut["name"]} 的最新作品")
        //        .WithDescription(Format.Url(title, $"https://www.pixiv.net/artworks/{jObject["id"]}") + "\n" + description)
        //        .WithUrl($"https://www.pixiv.net/users/{id}")
        //        .WithFooter(e.Author.Username + " ID: " + e.Author.Id, e.Author.GetAvatarUrl())
        //        .AddField("標籤", string.Join(", ", tags), true);

        //    if (e.GetGuild().Id != 463657254105645056)
        //    {
        //        if (tags.Contains("R-18"))
        //        {
        //            if (((ITextChannel)e.Channel).IsNsfw) discordEmbedBuilder.WithThumbnailUrl(thumbnailURL);
        //            else discordEmbedBuilder.WithThumbnailUrl("https://s.pximg.net/www/images/pixiv_logo.gif");
        //        }
        //        else discordEmbedBuilder.WithThumbnailUrl(thumbnailURL);
        //    }

        //    e.Channel.SendMessageAsync(null, false, discordEmbedBuilder.Build());
        //}

        private static async Task<(bool Status, IllustMetadata Reslut, string Error)> GetIllustDataFromAjaxAsync(string id)
        {
            string result = "";
            string error = "發生了未知的錯誤";

            try
            {
                result = await HttpClient.GetStringAsync($"https://www.pixiv.net/ajax/illust/{id}");
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, null, "找不到此Id的資料，可能已被刪除");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return (false, null, error);
            }

            var illust = JsonConvert.DeserializeObject<IllustMetadata>(result);

            if (illust == null)
            {
                return (false, null, "");
            }
            else if (illust.Error)
            {
                Log.Error(result);
                if (!string.IsNullOrWhiteSpace(illust.Message))
                    return (false, null, illust.Message);
            }

            return (true, illust, "");
        }
    }
}