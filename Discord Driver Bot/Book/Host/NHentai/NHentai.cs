using Discord;
using Discord.Commands;
using Discord_Driver_Bot.Command;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord_Driver_Bot.Book.Host.NHentai
{
    class NHentai
    {
        public static void GetData(string url, ICommandContext e)
        {
            if (!url.StartsWith("nhentai.net/g/")) return;

            string[] urlSplit = url.Split(new char[] { '?' })[0].Trim('/').Split(new char[] { '/' });
            string ID = urlSplit[2];
            if (!Function.GetIDIsExist(string.Format("https://nhentai.net/g/{0}", ID)))
            { e.Channel.SendMessageAsync(string.Format("{0} ID {1} 不存在本子", e.Message.Author.Mention, ID)); return; }

            try
            {
                string thumbnailURL, title, japanTitle, bookName;
                Dictionary<string, List<string>> dicTag;

                if (SQLite.SQLiteFunction.GetBookData(string.Format("https://nhentai.net/g/{0}", ID), out SQLite.Table.BookData bookData))
                {
                    thumbnailURL = bookData.ThumbnailUrl;
                    title = bookData.Title;
                    japanTitle = bookData.ExtensionData;
                    bookName = !string.IsNullOrEmpty(japanTitle) ? japanTitle.FormatBookName() : title.FormatBookName();
                    dicTag = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(bookData.Tags.Trim('"').Replace("\\", string.Empty));
                }
                else
                {
                    dicTag = new Dictionary<string, List<string>>();
                    Gallery gallery = API.GetGallery(ID);

                    thumbnailURL = $"https://t.nhentai.net/galleries/{gallery.MediaId}/cover.jpg";
                    title = gallery.Title.English;
                    japanTitle = gallery.Title.Japanese;
                    bookName = (gallery.Title.Pretty ?? gallery.Title.Japanese ?? gallery.Title.English).FormatBookName();

                    dicTag.Add("上傳時間", new List<string>() { DateTimeOffset.FromUnixTimeSeconds(gallery.UploadDate).ToString() });
                    dicTag.Add("喜歡人數", new List<string>() { gallery.NumFavorites.ToString() });
                    dicTag.Add("頁數", new List<string>() { gallery.NumPages.ToString() });

                    foreach (var item in gallery.Tags)
                    {
                        if (!dicTag.ContainsKey(item.Type))
                            dicTag.Add(item.Type, new List<string>() { item.Name + $" ({item.Count})" });
                        else
                            dicTag[item.Type].Add(item.Name + $" ({item.Count})");
                    }

                    new SQLite.Table.BookData(string.Format("https://nhentai.net/g/{0}", ID), title, japanTitle, thumbnailURL, dicTag).InsertNewData();
                }

                Log.FormatColorWrite(string.Format("{0} ({1})", thumbnailURL, bookName), ConsoleColor.Green);

                EmbedBuilder discordEmbedBuilder = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(title)
                    .WithDescription(japanTitle)
                    .WithUrl(string.Format("https://nhentai.net/g/{0}", ID))
                    .WithThumbnailUrl(e.Guild.Id == 463657254105645056 ? "" : thumbnailURL);

                foreach (var item in dicTag)
                    discordEmbedBuilder.AddField(item.Key, string.Join(", ", item.Value.Take(30)), true);                

                SearchFunction.SearchE_Hentai(bookName, out string E_HentaiUrl, out string E_HentaiLanguage);
                SearchFunction.SearchExHentai(bookName, out string ExHentaiUrl, out string ExHentaiLanguage);
                SearchFunction.SearchWnacg(bookName, out string wnacgUrl, out string wnacgLanguage);

                if (E_HentaiUrl != "" || wnacgUrl != "")
                {
                    discordEmbedBuilder.AddField("其他網站(不一定正確):",
                        (E_HentaiUrl != "" ? string.Format("[E-站({0})]({1})\t", E_HentaiLanguage, E_HentaiUrl) : "") +
                        (ExHentaiUrl != "" ? string.Format("[Ex站({0})]({1})\t", ExHentaiLanguage, ExHentaiUrl) : "") +
                        (wnacgUrl != "" ? string.Format("[W站({0})]({1})", wnacgLanguage, wnacgUrl) : ""), true);
                }
                else discordEmbedBuilder.AddField("其他網站:", "無", true);

                if (bookData != null) discordEmbedBuilder.AddField("被看過了",$"{bookData.DateTime.Replace("T", " ")} 被其他人看過", true);
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
