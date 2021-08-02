using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Driver_Bot.Command;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Html2Markdown;

namespace Discord_Driver_Bot.Book.Host.Pixiv
{
    static class Pixiv
    {
        public static void GetData(string url, ICommandContext e)
        {
            url = url.Split(new string[] { "&fb", "?fb", "&p", "?p" }, StringSplitOptions.RemoveEmptyEntries)[0];
            long id = url.FilterID();

            /*if (url.Contains("member.php") || url.Contains("users")) GetMenberData(id, e);
            else*/ if (url.Contains("member_illust.php") || url.Contains("artworks")) GetIllustData(id, e);
        }

        private static void GetIllustData(long id, ICommandContext e)
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
                var result = GetPixivData(id.ToString());
                var converter = new Converter();

                if (!result.Status) 
                {
                    Log.FormatColorWrite(result.Error, ConsoleColor.DarkRed);
                    return;
                }

                Body illust = result.Reslut.Body;
                title = illust.Title;
                description = illust.Description;
                description = converter.Convert(illust.Description);
                thumbnailURL = illust.Urls.Thumb.Replace("pximg.net", "pixiv.cat");
                tags = illust.Tags.Tags.Select((x) => x.Tag).ToList();
 
                new SQLite.Table.BookData($"https://www.pixiv.net/artworks/{id}", title, description, thumbnailURL, tags).InsertNewData();
            }

            Log.FormatColorWrite(thumbnailURL, ConsoleColor.Green);

            EmbedBuilder discordEmbedBuilder = new EmbedBuilder().WithOkColor()
                .WithTitle(title)
                .WithDescription(description)
                .WithUrl(string.Format("https://www.pixiv.net/artworks/{0}", id))
                .AddField("標籤", string.Join(", ", tags), true);

            if (e.Guild.Id != 463657254105645056)
            {
                if (tags.Contains("R-18"))
                {
                    if (((ITextChannel)e.Channel).IsNsfw) discordEmbedBuilder.WithThumbnailUrl(thumbnailURL);
                    else discordEmbedBuilder.WithThumbnailUrl("https://s.pximg.net/www/images/pixiv_logo.gif");
                }
                else discordEmbedBuilder.WithThumbnailUrl(thumbnailURL);
            }

            if (bookData != null) discordEmbedBuilder.AddField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過", true);
            discordEmbedBuilder.WithFooter(e.User.Username + " ID: " + e.User.Id, e.User.GetAvatarUrl());

            e.Channel.SendMessageAsync(null, false, discordEmbedBuilder.Build());
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

        private static (bool Status, IllustMetadata Reslut, string Error) GetPixivData(string id)
        {
            string result = "";
            string error = "發生了未知的錯誤";

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    result = webClient.DownloadString($"https://www.pixiv.net/ajax/illust/{id}");
                }
            }
            catch (Exception ex)
            {
                Log.FormatColorWrite(ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.DarkRed);
                return (false, null, error);
            }

            var illust = JsonConvert.DeserializeObject<IllustMetadata>(result);

            if (illust == null)
            {
                return (false, null, "");
            }
            else if (illust.Error)
            {
                Log.FormatColorWrite(result, ConsoleColor.DarkRed);
                if (!string.IsNullOrWhiteSpace( illust.Message))
                return (false, null, illust.Message);
            }

            return (true, illust, "");
        }
    }
}