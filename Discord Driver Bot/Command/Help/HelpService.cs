using Discord;
using Discord.Commands;
using System;
using System.Linq;

namespace Discord_Driver_Bot.Command.Help
{
    public class HelpService : ICommandService
    {
        public EmbedBuilder GetCommandHelp(CommandInfo com)
        {
            var prefix = "!!";

            var str = string.Format("**`{0}`**", prefix + com.Aliases.First());
            var alias = com.Aliases.Skip(1).FirstOrDefault();
            if (alias != null)
                str += string.Format(" **/ `{0}`**", prefix + alias);
            var em = new EmbedBuilder().AddField(fb => fb.WithName(str)
                                           .WithValue(com.Summary)
                                           .WithIsInline(true));

            if (com.Parameters.Count > 0)
            {
                string par = "";
                foreach (var item in com.Parameters)
                    par += item.Name + " " + item.Summary + "\n";
                em.AddField("參數", par.TrimEnd('\n'));
            }

            var reqs = GetCommandRequirements(com);
            if (reqs.Any()) em.AddField("要求", string.Join("\n", reqs));

            em.WithFooter(efb => efb.WithText("模組: " + com.Module.Name))
              .WithOkColor();

            return em;
        }

        public static string[] GetCommandRequirements(CommandInfo cmd) =>
            cmd.Preconditions
                  .Where(ca => ca is RequireOwnerAttribute || ca is RequireUserPermissionAttribute)
                  .Select(ca =>
                  {
                      if (ca is RequireOwnerAttribute)
                      {
                          return "Bot擁有者限定";
                      }

                      var cau = (RequireUserPermissionAttribute)ca;
                      if (cau.GuildPermission != null)
                      {
                          return ("伺服器 " + cau.GuildPermission.ToString() + " 權限")
                                       .Replace("Guild", "Server", StringComparison.InvariantCulture);
                      }

                      return ("頻道 " + cau.ChannelPermission + " 權限")
                                       .Replace("Guild", "Server", StringComparison.InvariantCulture);
                  })
                .ToArray();
    }
}
