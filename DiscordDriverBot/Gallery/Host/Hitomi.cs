using Discord;
using DiscordDriverBot.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace DiscordDriverBot.Gallery.Host
{
    class Hitomi
    {
        static Regex hashRegex = new Regex(@"[0-9a-f]{61}([0-9a-f]{2})([0-9a-f])");

        public static async Task GetDataAsync(string url, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            try
            {
                string[] urlSplit = url.Split(new string[] { "?", "#" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim('/').Split(new char[] { '/' });
                string id = urlSplit[2].Replace(".html", "");

                if (!Function.GetIDIsExist($"https://hitomi.la/galleries/{id}.html"))
                {
                    if (interactionContext == null)
                        await messageChannel.SendErrorAsync($"{user.Mention} ID {id.Split(new char[] { '.' })[0]} 不存在本子");
                    else
                        await interactionContext.Interaction.FollowupAsync($"ID {id.Split(new char[] { '.' })[0]} 不存在本子", ephemeral: true);
                    return;
                }

                string thumbnailURL = "", title = "", artist = "", bookName = "";
                Dictionary<string, List<string>> dicTag;

                if (SQLite.SQLiteFunction.GetBookData($"https://hitomi.la/galleries/{id}.html", out SQLite.Table.BookData bookData))
                {
                    thumbnailURL = bookData.ThumbnailUrl;
                    title = bookData.Title;
                    artist = bookData.ExtensionData;
                    bookName = title.Split(new char[] { '|' })[0].Trim().FormatBookName();
                    dicTag = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bookData.Tags);
                }
                else
                {
                    HttpClients.Hitomi.Gallery gallery = await Program.HitomiAPIClient.GetGalleryAsync(id);

                    var hashMatch = hashRegex.Match(gallery.Files.First().Hash);
                    //https://pixiv.cat/reverseproxy.html
                    thumbnailURL = $"https://hitomi-spoof.junrasp.com/avifsmallbigtn/{hashMatch.Groups[2]}/{hashMatch.Groups[1]}/{hashMatch.Value}.avif";

                    title = HttpUtility.HtmlDecode(gallery.Title);
                    artist = HttpUtility.HtmlDecode(gallery.Artists.First().Name);
                    bookName = title.Split(new char[] { '|' })[0].Trim().FormatBookName();

                    dicTag = new Dictionary<string, List<string>>();
                    if (gallery.Tags.Any((x) => !string.IsNullOrEmpty(x.Female)))
                        dicTag.Add("女性", gallery.Tags.Where((x) => !string.IsNullOrEmpty(x.Female)).Select((x) => x.Name).ToList());
                    if (gallery.Tags.Any((x) => !string.IsNullOrEmpty(x.Male)))
                        dicTag.Add("男性", gallery.Tags.Where((x) => !string.IsNullOrEmpty(x.Male)).Select((x) => x.Name).ToList());

                    new SQLite.Table.BookData(string.Format("https://hitomi.la/galleries/{0}", id), title, artist, thumbnailURL, dicTag).InsertNewData();
                }

                Log.New($"{thumbnailURL} ({bookName})");

                EmbedBuilder discordEmbedBuilder = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(title)
                    .WithDescription(artist)
                    .WithUrl($"https://hitomi.la/galleries/{id}.html");
                //.WithThumbnailUrl(guild.Id == 463657254105645056 ? "" : thumbnailURL);

                foreach (var item in dicTag)
                    discordEmbedBuilder.AddField(item.Key, string.Join("\n", item.Value), false);

                SearchSingle.SearchE_Hentai(bookName, out string E_HentaiUrl, out string E_HentaiLanguage);
                SearchSingle.SearchExHentai(bookName, out string ExHentaiUrl, out string ExHentaiLanguage);
                SearchSingle.SearchNHentai(bookName, out string nHentaiUrl, out string nHentaiLanguage);
                SearchSingle.SearchWnacg(bookName, out string wnacgUrl, out string wnacgLanguage);

                if (ExHentaiUrl != "" || nHentaiUrl != "" || wnacgUrl != "")
                {
                    discordEmbedBuilder.AddField("其他網站(不一定正確):",
                        (E_HentaiUrl != "" ? string.Format("[E-站({0})]({1})\t", E_HentaiLanguage, E_HentaiUrl) : "") +
                        (ExHentaiUrl != "" ? string.Format("[Ex站({0})]({1})\t", ExHentaiLanguage, ExHentaiUrl) : "") +
                        (nHentaiUrl != "" ? string.Format("[N站({0})]({1})\t", nHentaiLanguage, nHentaiUrl) : "") +
                        (wnacgUrl != "" ? string.Format("[W站({0})]({1})", wnacgLanguage, wnacgUrl) : ""), true);
                }
                else discordEmbedBuilder.AddField("其他網站:", "無", true);

                if (bookData != null) discordEmbedBuilder.AddField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過", true);
                discordEmbedBuilder.WithFooter(user.Username + " ID: " + user.Id, user.GetAvatarUrl());
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
                else
                    await Program.ApplicatonOwner.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithTitle($"{user.Username} ({guild.Name} ({guild.Id})/{messageChannel.Name} ({messageChannel.Id}))")
                        .WithUrl($"https://{url}")
                        .WithDescription(ex.ToString())
                        .Build());
#endif
                Log.Error(ex.ToString());
            }
        }
    }
} //https://btn.hitomi.la/avifsmallbigtn/a/6b/bd4585ff4aef873a1e70448ce4420b1adb1aa54e789a0bed6dfef192c51396ba.avif