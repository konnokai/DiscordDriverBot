using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordDriverBot.Command.Administration
{
    public class AdministrationService : ICommandService
    {
        private DiscordSocketClient _Client;
        public AdministrationService(DiscordSocketClient client)
        {
            _Client = client;
        }

        public async Task ClearUser(ITextChannel textChannel, ulong uId)
        {
            IEnumerable<IMessage> msgs = (await textChannel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false))
                .Where((item) => item.Author.Id == _Client.CurrentUser.Id && item.Embeds.Count > 0 &&
                item.Embeds.First().Footer.HasValue && item.Embeds.First().Footer.Value.Text.Contains(uId.ToString()));


            await Task.WhenAll(Task.Delay(1000), textChannel.DeleteMessagesAsync(msgs)).ConfigureAwait(false);
        }

        public async Task ClearUser(ITextChannel textChannel)
        {
            IEnumerable<IMessage> msgs = (await textChannel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false))
                  .Where((item) => item.Author.Id == _Client.CurrentUser.Id);

            await Task.WhenAll(Task.Delay(1000), textChannel.DeleteMessagesAsync(msgs)).ConfigureAwait(false);
        }
    }
}
