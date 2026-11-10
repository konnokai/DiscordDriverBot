using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordDriverBot.Gallery;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordDriverBot.Command
{
    public class CommandHandler : ICommandService
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
            var guild = (message.Channel as IGuildChannel)?.Guild;

            int argPos = 0;
            if (message.HasStringPrefix($"!!", ref argPos) || message.HasStringPrefix($"<@{Program._client.CurrentUser.Id}>", ref argPos) || message.HasStringPrefix($"<@!{Program._client.CurrentUser.Id}>", ref argPos))
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
                        Log.Error($"[{context.Guild?.Name ?? "DM"}/{context.Message.Channel.Name}] {message.Author.Username} 執行 {context.Message} 發生錯誤");
                        Log.Error(result.ErrorReason);
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                    }
                    else
                    {
                        if (guild != null &&
                            (context.Message.Author.Id == Program.ApplicatonOwner.Id || guild.Id == 429605944117297163) &&
                            !(context.Message.Content.StartsWith("!!sauce") && context.Message.Attachments.Count == 1))
                            await message.DeleteAsync();
                        Log.Info($"[{context.Guild?.Name ?? "DM"}/{context.Message.Channel.Name}] {message.Author.Username} 執行 {context.Message}");
                    }
                }
                else
                {
                    try
                    {
                        await HandelMessageAsync(message);

                    }
                    catch (Exception ex)
                    {
                        await message.Channel.SendErrorAsync(ex.Message);
                        Log.Error(ex.Demystify(), $"{context.Message.Channel} / {message.Author}: {message.Content}");
                    }
                }
            }
            else
            {
#if DEBUG
                await HandelMessageAsync(message);
#elif RELEASE
                try
                {
                    await HandelMessageAsync(message);
                }
                catch (Exception ex)
                {
                    await message.Channel.SendErrorAsync(ex.Message);
                    Log.Error(ex.Demystify(), $"{message.Channel} / {message.Author}: {message.Content}");
                }
#endif
            }
        }

        private async Task HandelMessageAsync(SocketUserMessage message)
        {
            var guild = (message.Channel as IGuildChannel)?.Guild;
            ITextChannel channel = message.Channel as ITextChannel;

            bool shown = false;

            foreach (var link in GalleryUrl.Extract(message.Content).Take(5))
            {
                if (await Gallery.Function.ShowGalleryInfoAsync(link, guild, message.Channel, message.Author) == GalleryShowResult.Shown)
                {
                    Log.FormatColorWrite($"[{guild.Name}/{channel.Name}]{message.Author.Username}: {link.SourceUrl}", ConsoleColor.Gray);
                    if (link.IsBook) SQLite.SQLiteFunction.UpdateGuildReadedBook(guild.Id);
                    shown = true;
                }
            }

            if (shown) await message.SuppressEmbedsAsync();
        }
    }
}
