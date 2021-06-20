using Discord;
using Discord.Commands;
using Discord_Driver_Bot.Command;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Discord_Driver_Bot.Book.Host
{
    public class Wnacg :IService
    {
        public static void GetData(string url, ICommandContext e)
        {
            if (url.Contains("?ctl"))
            {
                var array = HttpUtility.ParseQueryString(url.Split(new char[] { '?' })[1]);
                url = $"{array.Get("ctl")}-{array.Get("act")}-{array.GetKey(2)}-{array.Get(array.GetKey(2))}";
            }
            string[] urlSplit = url.Split(new char[] { '?' })[0].Split(new char[] { '-' });
            string ID = urlSplit[3].Split(new string[] { ".html" }, StringSplitOptions.RemoveEmptyEntries)[0];

            if (urlSplit[2] == "aid")
            {
                if (!Function.GetIDIsExist(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID)))
                { e.Channel.SendMessageAsync(string.Format("{0} ID {1} 不存在本子", e.Message.Author.Mention, ID.Split(new char[] { '.' })[0])); return; }
            }

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb(); IEnumerable<HtmlNode> htmlDocumentNode;
                if (urlSplit[1] == "view")
                {
                    htmlDocumentNode = htmlWeb.Load(string.Format("https://www.wnacg.com/photos-view-id-{0}.html", ID)).DocumentNode.Descendants();
                    urlSplit = htmlDocumentNode.First((x) => x.Name == "link" && x.Attributes.Any((x2) => x2.Name == "rel" && x2.Value == "alternate")).Attributes["href"].Value.Split(new char[] { '-' });
                    ID = urlSplit[3];
                }
                else if (urlSplit[2] == "page") ID = urlSplit[5];

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
                                        string[] temp = item2.InnerText.Split(new char[] { '：' });
                                        dicTag.Add(temp[0] + ":", new List<string>() { temp[1] });
                                    }
                                }
                                break;
                            case "div":
                                {
                                    List<string> list = new List<string>(item2.Descendants().Where((x) => x.Name == "a" && x.HasClass("tagshow")).Select((x) => x.InnerText.Trim()));
                                    if (list.Count > 0) dicTag.Add("標籤:", list);
                                }
                                break;
                            case "p":
                                {
                                    string[] temp = item2.InnerText.Split(new char[] { '：' });
                                    description = temp[1] != "" ? temp[1] : "這個上傳者很懶，甚麼都沒打";
                                }
                                break;
                        }
                    }

                    new SQLite.Table.BookData(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID), title, description, thumbnailURL, dicTag).InsertNewData();
                }

                Log.FormatColorWrite(string.Format("{0} ({1})", thumbnailURL, bookName), ConsoleColor.Green);

                EmbedBuilder discordEmbedBuilder = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(title)
                    .WithUrl(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", ID))
                    .WithDescription(description)
                    .WithThumbnailUrl(e.Guild.Id == 463657254105645056 ? "" : thumbnailURL);

                foreach (var item in dicTag)
                    discordEmbedBuilder.AddField(item.Key, string.Join(", ", item.Value), item.Key == "標籤:" ? false : true);                

                SearchFunction.SearchE_Hentai(bookName, out string E_HentaiUrl, out string E_HentaiLanguage);
                SearchFunction.SearchExHentai(bookName, out string ExHentaiUrl, out string ExHentaiLanguage);
                SearchFunction.SearchNHentai(bookName, out string nHentaiUrl, out string nHentaiLanguage);

                if (E_HentaiUrl != "" || nHentaiUrl != "")
                {
                    discordEmbedBuilder.AddField("其他網站(不一定正確):",
                        (E_HentaiUrl != "" ? string.Format("[E-站({0})]({1})\t", E_HentaiLanguage, E_HentaiUrl) : "") +
                        (ExHentaiUrl != "" ? string.Format("[Ex站({0})]({1})\t", ExHentaiLanguage, ExHentaiUrl) : "") +
                        (nHentaiUrl != "" ? string.Format("[N站({0})]({1})", nHentaiLanguage, nHentaiUrl) : ""), true);
                }
                else discordEmbedBuilder.AddField("其他網站:", "無", true);

                if (bookData != null) discordEmbedBuilder.AddField("被看過了", bookData.DateTime.Replace("T", " ") + " 被其他人看過", true);
                discordEmbedBuilder.WithFooter(e.Message.Author.Username + " ID: " + e.Message.Author.Id, e.Message.Author.GetAvatarUrl());
                e.Channel.SendMessageAsync(null, false, discordEmbedBuilder.Build());
            }
            catch (Exception e2)
            {
                Program.ApplicatonOwner.SendMessageAsync(string.Format("{0} ({1})\n{2}\n{3}", e.Message.Author.Username, e.Channel.Name, "https://" + url, e2.Message + "\n" + e2.StackTrace));
                Log.FormatColorWrite(e2.Message + "\r\n" + e2.StackTrace, ConsoleColor.Red);
            }
        }
    }
}
