using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Interaction.Twitter
{
    public class Twitter : TopLevelModule
    {
        [SlashCommand("convert-to-vxtwitter", "將網址轉換成 vxTwitter")]
        public async Task ConvertToVxTwitter([Summary("url", "網址")]string url)
        {
            var fixedUrl = url.Replace("twitter.com", "vxtwitter.com").Replace("x.com", "fixvx.com");
            await Context.Interaction.RespondAsync(fixedUrl);
        }

        [MessageCommand("轉換網址成 vxTwitter")]
        public async Task ConvertMessageToVxTwitter(IMessage message)
        {
            await Context.Interaction.SendConfirmAsync("Working...", false, true);

            var fixedMessage = message.Content.Replace("twitter.com", "vxtwitter.com").Replace("x.com", "fixvx.com");

            await Context.Channel.SendMessageAsync(fixedMessage, allowedMentions: AllowedMentions.None);
        }
    }
}
