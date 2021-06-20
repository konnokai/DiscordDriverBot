using Discord;
using Discord.Commands;
using Discord.WebSocket;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Command.Normal
{
    public class NormalService : IService
    {
        public NormalService()
        {
        }

        public async Task SearchNHentai(ICommandContext context, string bookName, int page)
        {
            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                string searchURL = string.Format("https://nhentai.net/search/?q={0}", bookName).Replace(" ", "+");
                if (page > 1) searchURL += "&page=" + page.ToString();
                
                IEnumerable<HtmlNode> htmlDocumentNode = htmlWeb.Load(searchURL).DocumentNode.Descendants();
                IEnumerable<HtmlNode> htmlNodes = htmlDocumentNode.Where((x) => x.Name == "div" && x.HasClass("gallery"));
                int searchCount = int.Parse(htmlDocumentNode.First((x) => x.Name == "h2").InnerText.Split(new char[] { ' ' })[0]);

                await context.SendPaginatedConfirmAsync(0, (row) =>
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                    .WithUrl(searchURL)
                    .WithTitle(string.Format("NHentai 搜尋 {0} 的結果如下", bookName))
                    .WithDescription($"共 {searchCount} 本，合計 {(searchCount / 25) + 1} 頁，目前為第 {page} 頁\n如需搜尋其他頁面請輸入以下指令\n**!!s \"{bookName}\" 頁數 n**");

                    foreach (HtmlNode item in htmlNodes.Skip(row * 5).Take(5))
                    {
                        if (item.Attributes.Any((x) => x.Name == "data-tags"))
                        {
                            string language = "";
                            if (item.Attributes.Any((x) => x.Value.Contains("29963"))) language = "中文";
                            else if (item.Attributes.Any((x) => x.Value.Contains("6346"))) language = "日文";
                            else language = "其他";

                            embedBuilder.AddField(item.Descendants().First((x) => x.HasClass("caption")).InnerText,
                                string.Format("[{0}]({1})", language, "https://nhentai.net" + item.FirstChild.Attributes["href"].Value), false);
                        }
                    }

                    return embedBuilder;
                }, htmlNodes.Count(), 5);
            }
            catch (Exception ex) { Log.FormatColorWrite(ex.Message, ConsoleColor.Red); }
        }

        public async Task SearchExHentai(ICommandContext context, string bookName, int page)
        {
            page--;
            try
            {
                string searchURL = $"https://exhentai.org/?f_search={bookName}&advsearch=1&f_sname=on&f_stags=on&f_sh=on&f_spf=&f_spt=".Replace(" ", "+");
                if (page > 0) searchURL += "&page=" + page.ToString();

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.Load(Book.Host.EHentai.API.GetExHentaiData(searchURL));
                var htmlDocumentNode = htmlDocument.DocumentNode.Descendants();

                IEnumerable<HtmlNode> htmlDocumentNode1 = htmlDocumentNode.Where((x) => x.HasClass("glink"));
                int searchCount = int.Parse(htmlDocumentNode.First((x) => x.HasClass("ip")).InnerText.Split(new char[] { ' ' })[1].Replace(",", ""));

                await context.SendPaginatedConfirmAsync(0, (row) =>
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                    .WithUrl(searchURL)
                    .WithTitle(string.Format("ExHentai 搜尋 {0} 的結果如下", bookName))
                    .WithDescription($"共 {searchCount} 本，合計 {(searchCount / 25) + 1} 頁，目前為第 {page + 1} 頁\n如需搜尋其他頁面請輸入以下指令\n**!!s \"{bookName}\" 頁數**");

                    foreach (HtmlNode item in htmlDocumentNode1.Skip(row * 5).Take(5))
                    {
                        string language = "";
                        if (item.InnerText.Contains("中国翻訳") || item.InnerText.Contains("中國翻訳")) language = "中文";
                        else if (item.InnerText.Contains("英訳")) language = "英文";
                        else if (item.InnerText.Contains("訳")) language = "其他";
                        else language = "日文";

                        embedBuilder.AddField(item.InnerText, $"[{language}]({item.ParentNode.GetAttributeValue("href", "")})", false);
                    }

                    return embedBuilder;
                }, htmlDocumentNode1.Count(), 5);
            }
            catch (Exception ex) { Log.FormatColorWrite(ex.Message, ConsoleColor.Red); }
        }
    }
}