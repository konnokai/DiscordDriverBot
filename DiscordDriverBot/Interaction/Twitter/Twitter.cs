using Discord;
using Discord.Interactions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordDriverBot.Interaction.Twitter
{
    public class Twitter : TopLevelModule
    {
        [SlashCommand("convert-to-vxtwitter", "將網址轉換成 vxTwitter")]
        public async Task ConvertToVxTwitter([Summary("url", "網址")] string url)
        {
            var fixedUrl = ToVxTwitter(url);
            await Context.Interaction.RespondAsync(fixedUrl, allowedMentions: AllowedMentions.None);
        }

        [MessageCommand("轉換網址成 vxTwitter")]
        public async Task ConvertMessageToVxTwitter(IMessage message)
        {
            var fixedMessage = ToVxTwitter(message.Content);
            await Context.Interaction.RespondAsync(fixedMessage, allowedMentions: AllowedMentions.None);
        }

        private static string ToVxTwitter(string text)
        {
            text = Regex.Replace(text, @"(?<![\w.-])(?:(?:www|mobile)\.)?twitter\.com(?![\w-])", "vxtwitter.com", RegexOptions.IgnoreCase);
            return Regex.Replace(text, @"(?<![\w.-])(?:www\.)?x\.com(?![\w-])", "fixvx.com", RegexOptions.IgnoreCase);
        }
    }
}
