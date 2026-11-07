using Discord;
using DiscordDriverBot.Command;
using DiscordDriverBot.SQLite.Table;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DiscordDriverBot.Gallery.Host.EHentai.TagTranslation;

namespace DiscordDriverBot.Gallery.Host.EHentai
{
    class EHentai
    {
        public static async Task<bool> GetDataAsync(GalleryLink link, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            if (link.Kind != GalleryLinkKind.Gallery && link.Kind != GalleryLinkKind.EHentaiPage) return false;

            string ID = link.Id, token = link.Token;
            bool isE_hentaiCanRead = false;
            HtmlWeb htmlWeb = new HtmlWeb(); HtmlDocument htmlDocument = new HtmlDocument();

            try
            {
                if (link.Kind == GalleryLinkKind.EHentaiPage)
                {
                    Tokenlist tokenListResult = await Program.EHentaiAPIClient.GetGalleryTokenAsync(int.Parse(link.Id), link.Token, link.Page);
                    ID = tokenListResult.gid.ToString(); token = tokenListResult.token;
                }

                try
                {
                    IEnumerable<HtmlNode> htmlDocumentNode = htmlWeb.Load(string.Format("https://e-hentai.org/g/{0}/{1}/", ID, token)).DocumentNode.Descendants();
                    isE_hentaiCanRead = !htmlDocumentNode.Any((x) => x.Name == "p" && x.InnerText.StartsWith("This gallery has been removed or is unavailable"));
                }
                catch (Exception) { }

                string thumbnailURL, title, japanTitle, bookName;
                Dictionary<string, List<string>> dicTag;

                if (SQLite.SQLiteFunction.GetBookData(string.Format("https://exhentai.org/g/{0}/{1}/", ID, token), out BookData bookData))
                {
                    thumbnailURL = bookData.ThumbnailUrl;
                    title = bookData.Title;
                    japanTitle = bookData.ExtensionData;
                    bookName = (japanTitle != "" ? japanTitle.FormatBookName() : title.FormatBookName());
                    dicTag = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bookData.Tags.Trim('"').Replace("\\", string.Empty));
                }
                else
                {
                    Gmetadata result = await Program.EHentaiAPIClient.GetGalleryMetadataAsync(int.Parse(ID), token);
                    dicTag = new Dictionary<string, List<string>>();
                    thumbnailURL = result.thumb;
                    title = result.title;
                    japanTitle = result.title_jpn;
                    bookName = japanTitle != "" ? japanTitle.FormatBookName() : title.FormatBookName();

                    dicTag.Add("上傳者", new List<string>() { result.uploader });
                    dicTag.Add("分類", new List<string>() { GetTranslatedTag("reclass", result.category.ToLower()) });
                    dicTag.Add("頁數", new List<string>() { result.filecount });

                    foreach (string item in result.tags)
                    {
                        string[] temp = item.Split(new char[] { ':' });
                        string nameSpace, tag;
                        List<string> tags = new List<string>();

                        if (temp.Length == 1) { nameSpace = "misc"; tag = temp[0]; }
                        else { nameSpace = temp[0]; tag = temp[1]; }

                        try
                        {
                            tag = GetTranslatedTag(nameSpace, tag, true);
                            nameSpace = GetTranslatedTag("rows", nameSpace);
                        }
                        catch (Exception) { }

                        if (dicTag.ContainsKey(nameSpace)) dicTag[nameSpace].Add(tag);
                        else dicTag.Add(nameSpace, new List<string>() { tag });
                    }

                    new BookData(string.Format("https://exhentai.org/g/{0}/{1}/", ID, token), title, japanTitle, thumbnailURL, dicTag).InsertNewData();
                }

                Log.New($"{thumbnailURL} ({bookName})");

                GalleryCard card = GalleryMessage.Card()
                    .WithHeader(title, string.Format("https://exhentai.org/g/{0}/{1}/", ID, token));

                if (!string.IsNullOrWhiteSpace(japanTitle))
                    card.Text(japanTitle);

                foreach (KeyValuePair<string, List<string>> item in dicTag)
                    card.WithField(item.Key, string.Join(", ", item.Value));

                SearchSingle.SearchNHentai(title.FormatBookName(), out string nHentaiUrl, out string nHentaiLanguage);
                SearchSingle.SearchWnacg(bookName, out string wnacgUrl, out string wnacgLanguage);

                card.WithField("其他網站(不一定正確):",
                    (string.Format("[表站{0}]({1})\t", isE_hentaiCanRead ? "" : "(需要銅星贊助)", string.Format("https://e-hentai.org/g/{0}/{1}/", ID, token))) +
                    (nHentaiUrl != "" ? string.Format("[N站({0})]({1})\t", nHentaiLanguage, nHentaiUrl) : "") +
                    (wnacgUrl != "" ? string.Format("[W站({0})]({1})", wnacgLanguage, wnacgUrl) : ""));

                if (bookData != null) card.WithField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過");

                if (guild.Id != 463657254105645056 && !string.IsNullOrWhiteSpace(thumbnailURL))
                    card.WithMediaGallery(new MediaGalleryBuilder().AddItem(thumbnailURL));

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
                        .WithDescription($"你在 {guild.Name}/{messageChannel.Name} 使用到了Bot的功能，但Bot無讀取&發言&嵌入連結權限\n請向管理員要求提供Bot權限")
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

                if (interactionContext != null)
                    await interactionContext.Interaction.FollowupAsync($"{user.Mention} 解析失敗，請確認網址是否正確", ephemeral: true);

                return false;
            }
        }
    }
}