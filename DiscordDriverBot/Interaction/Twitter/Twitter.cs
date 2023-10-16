using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Interaction.Twitter
{
    public class Twitter : TopLevelModule
    {
        [MessageCommand("轉換網址成 vxTwitter")]
        public async Task ConvertToVxTwitter(IMessage message)
        {
            await Context.Interaction.SendConfirmAsync("Working...", false, true);

            var fixedMessage = message.CleanContent.Replace("twitter.com", "vxtwitter.com").Replace("x.com", "fixvx.com");

            await Context.Channel.SendMessageAsync(fixedMessage, allowedMentions: AllowedMentions.None);
        }
    }
}
