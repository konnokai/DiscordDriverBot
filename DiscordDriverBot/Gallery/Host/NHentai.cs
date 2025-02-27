﻿using Discord;
using DiscordDriverBot.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordDriverBot.Gallery.Host
{
    class NHentai
    {
        public static async Task GetDataAsync(string url, IGuild guild, IMessageChannel messageChannel, IUser user, IInteractionContext interactionContext)
        {
            if (!url.StartsWith("nhentai.net/g/")) return;

            string[] urlSplit = url.Split(new char[] { '?' })[0].Trim('/').Split(new char[] { '/' });
            string ID = urlSplit[2];
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
                    HttpClients.NHentai.Gallery gallery = await Program.NHentaiAPIClient.GetGalleryAsync(ID);

                    if (gallery == null)
                    {
                        if (interactionContext == null)
                            await messageChannel.SendErrorAsync($"{user.Mention} ID {ID.Split(new char[] { '.' })[0]} 不存在本子");
                        else
                            await interactionContext.Interaction.FollowupAsync($"ID {ID.Split(new char[] { '.' })[0]} 不存在本子", ephemeral: true);
                        return;
                    }

                    thumbnailURL = $"https://t3.nhentai.net/galleries/{gallery.MediaId}/cover.jpg";
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

                Log.New($"{thumbnailURL} ({bookName})");

                EmbedBuilder discordEmbedBuilder = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(title)
                    .WithDescription(japanTitle)
                    .WithUrl(string.Format("https://nhentai.net/g/{0}", ID))
                    .WithThumbnailUrl(guild.Id == 463657254105645056 ? "" : thumbnailURL);

                foreach (var item in dicTag)
                    discordEmbedBuilder.AddField(item.Key, string.Join(", ", item.Value.Take(30)), true);

                SearchSingle.SearchE_Hentai(bookName, out string E_HentaiUrl, out string E_HentaiLanguage);
                SearchSingle.SearchExHentai(bookName, out string ExHentaiUrl, out string ExHentaiLanguage);
                SearchSingle.SearchWnacg(bookName, out string wnacgUrl, out string wnacgLanguage);

                if (ExHentaiUrl != "" || wnacgUrl != "")
                {
                    discordEmbedBuilder.AddField("其他網站(不一定正確):",
                        (E_HentaiUrl != "" ? string.Format("[E-站({0})]({1})\t", E_HentaiLanguage, E_HentaiUrl) : "") +
                        (ExHentaiUrl != "" ? string.Format("[Ex站({0})]({1})\t", ExHentaiLanguage, ExHentaiUrl) : "") +
                        (wnacgUrl != "" ? string.Format("[W站({0})]({1})", wnacgLanguage, wnacgUrl) : ""), true);
                }
                else discordEmbedBuilder.AddField("其他網站:", "無", true);

                if (bookData != null) discordEmbedBuilder.AddField("被看過了", $"{bookData.DateTime.Replace("T", " ")} 被其他人看過", true);
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
                {
                    await user.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription($"你在 {guild.Name}/{messageChannel.Name} 使用到了Bot的功能，但Bot無讀取&發言&嵌入連結權限\n請向管理員要求提供Bot權限")
                        .Build());
                }
                else if (ex.Message.Contains("503"))
                {
                    var embed = new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription($"NHentai API伺服器無法使用，如需隱藏此錯誤請在網址前新增`#`")
                        .Build();
                    if (interactionContext == null)
                        await messageChannel.SendMessageAsync(embed: embed);
                    else
                        await interactionContext.Interaction.FollowupAsync(embed: embed);
                }
                else
                {
                    await Program.ApplicatonOwner.SendMessageAsync(embed: new EmbedBuilder()
                        .WithErrorColor()
                        .WithTitle($"{user.Username} ({guild.Name} ({guild.Id})/{messageChannel.Name} ({messageChannel.Id}))")
                        .WithUrl($"https://{url}")
                        .WithDescription(ex.ToString())
                        .Build());
                    Log.Error(ex.ToString());
                }
#endif
            }
        }
    }
}