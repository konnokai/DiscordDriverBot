using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Command
{
    public class CommandHandler : IService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        private ulong[] judgeList = new ulong[] { 568154286672969770, 566998648873811968, 499273835036803082, 535976499820494850, 599685047037198336 };

        public CommandHandler(IServiceProvider services, CommandService commands, DiscordSocketClient client)
        {
            _commands = commands;
            _services = services;
            _client = client;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);
            _client.MessageReceived += (msg) => { var _ = Task.Run(() => HandleCommandAsync(msg)); return Task.CompletedTask; };
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null || message.Author.IsBot) return;
            var guild = message.GetGuild();

            int argPos = 0;
            if (message.HasStringPrefix("!!", ref argPos))
            {
                var context = new SocketCommandContext(_client, message);

                if (_commands.Search(context, argPos).IsSuccess)
                {
                    var result = await _commands.ExecuteAsync(
                        context: context,
                        argPos: argPos,
                        services: _services);

                    if (!result.IsSuccess)
                    {
                        Log.FormatColorWrite($"[{context.Guild.Name}/{context.Message.Channel.Name}] {message.Author.Username} 執行 {context.Message} 發生錯誤", ConsoleColor.Red);
                        Log.FormatColorWrite(result.ErrorReason, ConsoleColor.Red);
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                    }
                    else
                    {
                        if ((context.Message.Author.Id == Program.ApplicatonOwner.Id || guild.Id == 429605944117297163) &&
                            !(context.Message.Content.StartsWith("!!sauce") && context.Message.Attachments.Count == 1))
                            await message.DeleteAsync();
                        Log.FormatColorWrite($"[{context.Guild.Name}/{context.Message.Channel.Name}] {message.Author.Username} 執行 {context.Message}", ConsoleColor.DarkYellow);
                    }
                }
            }
            else
            {
#if DEBUG
                foreach (string item in message.Content.Split(new char[] { '\n' }))
                {
                    Book.Function.ShowBookInfo(item, new SocketCommandContext(_client, message));
                }
#elif RELEASE
                string content = message.Content;
                ITextChannel channel = message.Channel as ITextChannel;
                IGuildUser guildUser = message.Author as IGuildUser;

                if (content == "<:notify:314000626608504832>")
                {
                    await channel.SendMessageAsync("<:notify:314000626608504832><:notify:314000626608504832><:notify:314000626608504832><:notify:314000626608504832><:notify:314000626608504832>");
                    return;
                }

                if (message.MentionedUsers.Any((x) => x.Id == Program._client.CurrentUser.Id) && message.MentionedUsers.Count == 1)
                {
                    await message.Channel.TriggerTypingAsync();
                    Random random = new Random(); string rndHost = "", rndId = "";

                    switch (random.Next(0, 3))
                    {
                        case 0:
                            rndHost = "n";

                            do
                            { rndId = random.Next(1, 350000).ToString(); }
                            while (!Book.Function.GetIDIsExist(string.Format("https://nhentai.net/g/{0}/", rndId)));

                            break;
                        case 1:
                            rndHost = "w";

                            do
                            { rndId = random.Next(1, 100000).ToString(); }
                            while (!Book.Function.GetIDIsExist(string.Format("https://www.wnacg.com/photos-index-aid-{0}.html", rndId)));

                            break;
                        case 2:
                            rndHost = "p";

                            do
                            { rndId = random.Next(1, 80000000).ToString(); }
                            while (!Book.Function.GetIDIsExist(string.Format("https://www.pixiv.net/artworks/{0}", rndId)));

                            break;
                        case 3:
                            rndHost = "h";

                            do
                            { rndId = random.Next(800000, 1500000).ToString(); }
                            while (!Book.Function.GetIDIsExist(string.Format("https://hitomi.la/galleries/{0}.html", rndId)));

                            break;
                    }

                    await message.Channel.SendMessageAsync(rndHost + " " + rndId);
                }

                #region 舞池處理
                if (guild.Id == 463657254105645056 && judgeList.Contains(channel.CategoryId.Value) &&
                    guildUser.Id != guildUser.Guild.OwnerId && !guildUser.RoleIds.Any(x => x == 464047563839111168 || x == 544581212296052756))
                {
                    SocketTextChannel textChannel = await channel.Guild.GetChannelAsync(463657254105645058) as SocketTextChannel;
                    if (channel.Name.Contains("貼圖"))
                    {
                        if (message.Content == string.Empty) return;

                        foreach (string item in content.Split(new char[] { '\n' }))
                        {
                            try
                            {
                                string url = Book.Function.FilterUrl(item);
                                Book.Function.BookHost host = Book.Function.CheckBookHost(url);
                                if (host != Book.Function.BookHost.None && host != Book.Function.BookHost.Pixiv)
                                {
                                    await textChannel.SendMessageAsync(string.Format("{0} 不要在貼圖舞池貼本 ({1})", guildUser.Mention, channel.Mention));
                                    Log.FormatColorWrite(string.Format("{0} 在舞池貼本 ({1}): {2}", guildUser.Username, channel.Name, content), ConsoleColor.DarkRed);
                                    return;
                                }
                                else if (!url.StartsWith("http") && message.Attachments.Count == 0)
                                {
                                    await textChannel.SendMessageAsync(string.Format("{0} 不要在貼圖舞池聊天 ({1})\n說了: {2}", guildUser.Mention, channel.Mention, content));
                                    Log.FormatColorWrite(string.Format("{0} 在舞池聊天 ({1})\n說了: {2}", guildUser.Username, channel.Name, content), ConsoleColor.DarkRed);
                                    return;
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    else if (channel.CategoryId == 499273835036803082 || channel.CategoryId == 599685047037198336)
                    {
                        if (content != string.Empty)
                        {
                            bool FUCKYOU = true;
                            foreach (string item in content.Split(new char[] { '\n' }))
                                if (Book.Function.FilterUrl(item).StartsWith("http")) FUCKYOU = false;

                            if (FUCKYOU)
                            {
                                await textChannel.SendMessageAsync(string.Format("{0} 不要在舞池聊天 ({1})\n說了: {2}", guildUser.Mention, channel.Mention, content));
                                Log.FormatColorWrite(string.Format("{0} 在舞池聊天 ({1})\n說了: {2}", guildUser.Username, channel.Name, content), ConsoleColor.DarkRed);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (content == string.Empty)
                        {
                            await textChannel.SendMessageAsync(string.Format("{0} 不要在該舞池貼圖 ({1})", guildUser.Mention, channel.Mention));
                            return;
                        }

                        bool FUCKYOU = true;
                        foreach (string item in content.Split(new char[] { '\n' }))
                            if (Book.Function.FilterUrl(item).StartsWith("http")) FUCKYOU = false;

                        if (FUCKYOU)
                        {
                            await textChannel.SendMessageAsync(string.Format("{0} 不要在舞池聊天 ({1})\n說了: {2}", guildUser.Mention, channel.Mention, content));
                            Log.FormatColorWrite(string.Format("{0} 在舞池聊天 ({1})\n說了: {2}", guildUser.Username, channel.Name, content), ConsoleColor.DarkRed);
                            return;
                        }
                    }
                }
                #endregion

                if (content.Contains("#http") || content.Contains("<http") || content.Contains("||http")) return;

                foreach (string item in content.Split(new char[] { '\n' }))
                {
                    if (Book.Function.ShowBookInfo(item, new SocketCommandContext(_client, message)))
                    {
                        Log.FormatColorWrite($"[{guild.Name}/{channel.Name}]{guildUser.Username}: {item}");
                        SQLite.SQLiteFunction.UpdateGuildReadedBook(guild.Id);
                    }
                }
#endif
            }
        }
    }
}
