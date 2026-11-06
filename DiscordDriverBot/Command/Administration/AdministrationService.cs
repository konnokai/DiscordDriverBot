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
                .Where((item) => item.Author.Id == _Client.CurrentUser.Id && MentionedUser(item, uId));


            await Task.WhenAll(Task.Delay(1000), textChannel.DeleteMessagesAsync(msgs)).ConfigureAwait(false);
        }

        // 舊卡片把使用者 ID 放在 embed footer，新卡片（Components V2）放在 TextDisplay 裡
        private static bool MentionedUser(IMessage message, ulong uId)
        {
            string tag = $"ID: {uId}";

            if (message.Embeds.FirstOrDefault()?.Footer?.Text?.EndsWith(tag) == true) return true;

            return Flatten(message.Components)
                .OfType<TextDisplayComponent>()
                .Any((x) => x.Content.EndsWith(tag));
        }

        private static IEnumerable<IMessageComponent> Flatten(IEnumerable<IMessageComponent> components)
        {
            foreach (IMessageComponent component in components)
            {
                yield return component;

                IEnumerable<IMessageComponent> children = component switch
                {
                    ContainerComponent container => container.Components,
                    SectionComponent section => section.Components,
                    ActionRowComponent actionRow => actionRow.Components,
                    _ => null
                };

                if (children == null) continue;

                foreach (IMessageComponent child in Flatten(children)) yield return child;
            }
        }

        public async Task ClearUser(ITextChannel textChannel)
        {
            IEnumerable<IMessage> msgs = (await textChannel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false))
                  .Where((item) => item.Author.Id == _Client.CurrentUser.Id);

            await Task.WhenAll(Task.Delay(1000), textChannel.DeleteMessagesAsync(msgs)).ConfigureAwait(false);
        }
    }
}
