using Discord;
using DiscordDriverBot.Command;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace DiscordDriverBot.Gallery.Host
{
    public class Wnacg : ICommandService
    {
        public static async Task<bool> GetDataAsync(GalleryLink link, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            if (link.Kind != GalleryLinkKind.Gallery && link.Kind != GalleryLinkKind.WnacgView) return false;

            string ID = link.Id;

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb(); IEnumerable<HtmlNode> htmlDocumentNode;

                if (link.Kind == GalleryLinkKind.WnacgView)
                {
                    htmlDocumentNode = htmlWeb.Load($"https://www.wnacg.com/photos-view-id-{ID}.html").DocumentNode.Descendants();
                    string href = htmlDocumentNode
                        .Where((x) => x.Name == "link" && x.Attributes.Any((x2) => x2.Name == "rel" && x2.Value == "alternate"))
                        .Select((x) => x.GetAttributeValue("href", ""))
                        .FirstOrDefault((x) => Regex.IsMatch(x, @"-aid-\d{1,9}\.html$"));

                    Match match = Regex.Match(href ?? "", @"-aid-(?<id>\d{1,9})\.html$");
                    if (!match.Success)
                    {
                        if (interactionContext == null)
                            await messageChannel.SendErrorAsync($"{user.Mention} 找不到此本子");
                        else
                            await interactionContext.Interaction.FollowupAsync($"{user.Mention} 找不到此本子", ephemeral: true);
                        return false;
                    }

                    ID = match.Groups["id"].Value;
                }
                else if (!Function.GetIDIsExist($"https://www.wnacg.com/photos-index-aid-{ID}.html"))
                {
                    if (interactionContext == null)
                        await messageChannel.SendErrorAsync($"{user.Mention} ID {ID} 不存在本子");
                    else
                        await interactionContext.Interaction.FollowupAsync($"ID {ID} 不存在本子", ephemeral: true);
                    return false;
                }

                string thumbnailURL, title, description = "", bookName;
                Dictionary<string, List<string>> dicTag;

                if (SQLite.SQLiteFunction.GetBookData(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID), out SQLite.Table.BookData bookData))
                {
                    thumbnailURL = bookData.ThumbnailUrl;
                    title = bookData.Title;
                    description = bookData.ExtensionData;
                    bookName = title.FormatBookName();
                    dicTag = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bookData.Tags.Trim('"').Replace("\\", string.Empty));
                }
                else
                {
                    dicTag = new Dictionary<string, List<string>>();
                    htmlDocumentNode = htmlWeb.Load(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID)).DocumentNode.Descendants();

                    title = HttpUtility.HtmlDecode(htmlDocumentNode.First((x) => (x.ParentNode.Name == "div" && x.ParentNode.Id == "bodywrap" && x.Name == "h2")).InnerText);
                    thumbnailURL = "https://" + htmlDocumentNode.First((x) => (x.ParentNode.Name == "div" && x.ParentNode.HasClass("uwthumb") && x.Name == "img")).GetAttributeValue("src", "").TrimStart('/');
                    bookName = title.FormatBookName();

                    foreach (HtmlNode item2 in htmlDocumentNode.First((x) => x.Name == "div" && x.HasClass("uwconn")).Descendants())
                    {
                        switch (item2.Name)
                        {
                            case "label":
                                {
                                    if (item2.ParentNode.ParentNode.HasClass("asTB"))
                                    {
                                        string[] temp = item2.InnerText.Split(['：']);
                                        dicTag.Add(temp[0] + ":", [temp[1]]);
                                    }
                                }
                                break;
                            case "div":
                                {
                                    List<string> list = new(item2.Descendants().Where((x) => x.Name == "a" && x.HasClass("tagshow")).Select((x) => x.InnerText.Trim()));
                                    if (list.Count > 0) dicTag.Add("標籤:", list);
                                }
                                break;
                            case "p":
                                {
                                    string[] temp = item2.InnerText.Split(['：']);
                                    description = temp[1] != "" ? temp[1] : "這個上傳者很懶，甚麼都沒打";
                                }
                                break;
                        }
                    }

                    new SQLite.Table.BookData(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID), title, description, thumbnailURL, dicTag).InsertNewData();
                }

                Log.New($"{thumbnailURL} ({bookName})");

                GalleryCard card = GalleryMessage.Card()
                    .WithHeader(title, string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID), guild.Id == 463657254105645056 ? null : thumbnailURL);

                if (!string.IsNullOrWhiteSpace(description))
                    card.Text(description);

                foreach (var item in dicTag)
                    card.WithField(item.Key, string.Join(", ", item.Value));

                SearchSingle.SearchE_Hentai(bookName, out string E_HentaiUrl, out string E_HentaiLanguage);
                SearchSingle.SearchExHentai(bookName, out string ExHentaiUrl, out string ExHentaiLanguage);
                SearchSingle.SearchNHentai(bookName, out string nHentaiUrl, out string nHentaiLanguage);

                if (ExHentaiUrl != "" || nHentaiUrl != "")
                {
                    card.WithField("其他網站(不一定正確):",
                        (E_HentaiUrl != "" ? string.Format("[E-站({0})]({1})\t", E_HentaiLanguage, E_HentaiUrl) : "") +
                        (ExHentaiUrl != "" ? string.Format("[Ex站({0})]({1})\t", ExHentaiLanguage, ExHentaiUrl) : "") +
                        (nHentaiUrl != "" ? string.Format("[N站({0})]({1})", nHentaiLanguage, nHentaiUrl) : ""));
                }
                else card.WithField("其他網站:", "無");

                if (bookData != null) card.WithField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過");
                card.WithFooter(user);

                await card.SendAsync(messageChannel, interactionContext);

                return true;
            }
            catch (Exception ex)
            {
#if RELEASE
                if (ex.Message.Contains("50013"))
                    await user.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription($"你在 {guild.Name}/{messageChannel.Name} 使用到了 Bot 的功能，但 Bot 無讀取 & 發言 & 嵌入連結權限\n請向管理員要求提供 Bot 權限")
                        .Build());
                else
                    await Program.ApplicatonOwner.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithTitle($"{user.Username} ({guild.Name} ({guild.Id})/{messageChannel.Name} ({messageChannel.Id}))")
                        .WithUrl(link.SourceUrl)
                        .WithDescription(ex.ToString())
                        .Build());
#endif
                Log.Error(ex.ToString());

                EmbedBuilder discordEmbedBuilder = new EmbedBuilder()
                    .WithErrorColor()
                    .WithTitle("發生錯誤")
                    .WithDescription("發生未知的錯誤，已回報給 Bot 擁有者處理");

                if (interactionContext == null)
                    await messageChannel.SendMessageAsync(embed: discordEmbedBuilder.Build());
                else
                    await interactionContext.Interaction.FollowupAsync(embed: discordEmbedBuilder.Build());

                return false;
            }
        }
    }
}
