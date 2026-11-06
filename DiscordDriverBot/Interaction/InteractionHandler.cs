using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

#if DEBUG
using System.Text.RegularExpressions;
#endif

namespace DiscordDriverBot.Interaction
{
    class InteractionHandler : IInteractionService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        public string CommandSignature => BuildCommandSignature();

        private string BuildCommandSignature()
        {
            var sb = new StringBuilder();

            foreach (var module in _interactions.Modules
                .Where((x) => x.IsSlashGroup)
                .OrderBy(ModulePath, StringComparer.Ordinal))
            {
                sb.Append("group|").Append(ModulePath(module)).Append('|')
                  .Append(module.Description).Append('|')
                  .Append(Scope(null, module.DefaultMemberPermissions, module.ContextTypes, module.IntegrationTypes, module.IsNsfw, module.IsEnabledInDm))
                  .Append('\n');
            }

            foreach (var cmd in _interactions.SlashCommands
                .OrderBy((x) => CommandPath(x.Module, x.Name), StringComparer.Ordinal))
            {
                sb.Append("slash|").Append(CommandPath(cmd.Module, cmd.Name)).Append('|')
                  .Append(cmd.Description).Append('|')
                  .Append(Scope(cmd.DefaultMemberPermissions, cmd.Module.DefaultMemberPermissions, cmd.ContextTypes, cmd.IntegrationTypes, cmd.IsNsfw, cmd.IsEnabledInDm))
                  .Append('|');

                // 參數順序就是 Discord 註冊選項的順序，不能排序
                foreach (var p in cmd.FlattenedParameters)
                {
                    sb.Append(p.Name).Append(':').Append(p.DiscordOptionType).Append(':')
                      .Append(p.IsRequired ? "R" : "O").Append(':').Append(p.Description).Append(':')
                      .Append(p.MinValue?.ToString(CultureInfo.InvariantCulture)).Append(':')
                      .Append(p.MaxValue?.ToString(CultureInfo.InvariantCulture)).Append(':')
                      .Append(p.MinLength).Append(':').Append(p.MaxLength).Append(':')
                      .Append(p.IsAutocomplete ? "A" : "-").Append(':')
                      .Append(string.Join(",", (p.ChannelTypes?.Select((x) => x.ToString()) ?? Enumerable.Empty<string>()).OrderBy((x) => x, StringComparer.Ordinal))).Append(':');
                    foreach (var choice in GetChoices(p).OrderBy((x) => x.Name, StringComparer.Ordinal))
                        sb.Append(choice.Name).Append('=').Append(choice.Value).Append(',');
                    sb.Append(';');
                }
                sb.Append('\n');
            }

            foreach (var cmd in _interactions.ContextCommands
                .OrderBy((x) => x.CommandType).ThenBy((x) => x.Name, StringComparer.Ordinal))
                sb.Append("context|").Append(cmd.CommandType).Append('|').Append(cmd.Name).Append('|')
                  .Append(Scope(cmd.DefaultMemberPermissions, cmd.Module.DefaultMemberPermissions, cmd.ContextTypes, cmd.IntegrationTypes, cmd.IsNsfw, cmd.IsEnabledInDm))
                  .Append('\n');

            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        private static string ModulePath(ModuleInfo module)
        {
            var parts = new List<string>();
            for (var current = module; current != null && current.IsSlashGroup; current = current.Parent)
                parts.Insert(0, current.SlashGroupName);
            return string.Join(".", parts);
        }

        private static string CommandPath(ModuleInfo module, string name)
        {
            string group = ModulePath(module);
            return string.IsNullOrEmpty(group) ? name : group + "." + name;
        }

        private static string Scope(GuildPermission? commandPermissions, GuildPermission? modulePermissions,
            IEnumerable<InteractionContextType> contextTypes, IEnumerable<ApplicationIntegrationType> integrationTypes, bool isNsfw, bool isEnabledInDm) =>
            string.Join("|", ((commandPermissions ?? 0) | (modulePermissions ?? 0)).ToString(),
                string.Join(",", (contextTypes?.Select((x) => x.ToString()) ?? Enumerable.Empty<string>()).OrderBy((x) => x, StringComparer.Ordinal)),
                string.Join(",", (integrationTypes?.Select((x) => x.ToString()) ?? Enumerable.Empty<string>()).OrderBy((x) => x, StringComparer.Ordinal)),
                isNsfw, isEnabledInDm);

        private static IEnumerable<(string Name, string Value)> GetChoices(SlashCommandParameterInfo parameter)
        {
            foreach (var choice in parameter.Choices)
                yield return (choice.Name, Convert.ToString(choice.Value, CultureInfo.InvariantCulture));

            Type enumType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            if (!enumType.IsEnum) yield break;

            foreach (var member in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (member.IsDefined(typeof(HideAttribute), true)) continue;
                string name = member.GetCustomAttribute<ChoiceDisplayAttribute>()?.Name ?? member.Name;
                // Discord.Net 註冊 enum 選項時用成員名稱當 value
                yield return (name, member.Name);
            }
        }

        public InteractionHandler(IServiceProvider services, InteractionService interactions, DiscordSocketClient client)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _interactions.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);

            #region 檢查指令是否符合Discord的Regex規範
#if DEBUG
            bool isError = false;
            Regex regex = new Regex(@"^[\w-]{1,32}$");
            var list = _interactions.Modules.Select(module => module.SlashCommands.Select((x) => new KeyValuePair<string, List<string>>(x.Name, x.Parameters.Select((x2) => x2.Name).ToList())));
            foreach (var item in list)
            {
                foreach (var item2 in item)
                {
                    if (!regex.IsMatch(item2.Key))
                        Log.Error(item2.Key);
                    foreach (var item3 in item2.Value)
                    {
                        if (!regex.IsMatch(item3))
                        {
                            isError = true;
                            Log.Error($"{item2.Key}: {item3}");
                        }
                    }
                }
            }
            if (isError) return;
#endif
            #endregion

            _client.InteractionCreated += (slash) => { var _ = Task.Run(() => HandleInteraction(slash)); return Task.CompletedTask; };
            _client.MessageCommandExecuted += MessageCommandExecuted;
            _interactions.SlashCommandExecuted += SlashCommandExecuted;
        }

        private Task MessageCommandExecuted(SocketMessageCommand arg)
        {
            Log.Info($"[{arg.GetGuild()}/{arg.Channel.Name}] {arg.User.Username} 執行訊息指令 `{arg.CommandName}`");
            return Task.CompletedTask;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                await _interactions.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            string slashCommand = $"/{arg1}";
            var commandData = arg2.Interaction.Data as SocketSlashCommandData;
            if (commandData.Options.Count > 0) slashCommand += GetOptionsValue(commandData.Options.First());

            if (arg3.IsSuccess)
            {
                Log.Info($"[{arg2.Guild.Name}/{arg2.Channel.Name}] {arg2.User.Username} 執行 `{slashCommand}`");
            }
            else
            {
                Log.Error($"[{arg2.Guild.Name}/{arg2.Channel.Name}] {arg2.User.Username} 執行 `{slashCommand}` 發生錯誤\r\n{arg3.ErrorReason}");
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        arg2.Interaction.SendErrorAsync(arg3.ErrorReason);
                        break;
                    case InteractionCommandError.UnknownCommand:
                        arg2.Interaction.SendErrorAsync("未知的指令，也許被移除或變更了");
                        break;
                    case InteractionCommandError.BadArgs:
                        arg2.Interaction.SendErrorAsync("輸入的參數錯誤");
                        break;
                    default:
                        arg2.Interaction.SendErrorAsync("未知的錯誤，請向Bot擁有者回報");
                        break;
                }
            }

            return Task.CompletedTask;
        }

        private string GetOptionsValue(SocketSlashCommandDataOption socketSlashCommandDataOption)
        {
            try
            {
                if (socketSlashCommandDataOption.Type != ApplicationCommandOptionType.SubCommand && socketSlashCommandDataOption.Type != ApplicationCommandOptionType.SubCommandGroup && !socketSlashCommandDataOption.Options.Any())
                    return $" {socketSlashCommandDataOption.Value}";

                if (socketSlashCommandDataOption.Type == ApplicationCommandOptionType.SubCommand || socketSlashCommandDataOption.Type == ApplicationCommandOptionType.SubCommandGroup) GetOptionsValue(socketSlashCommandDataOption.Options.First());
                return " " + string.Join(' ', socketSlashCommandDataOption.Options.Select(option => option.Value));
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
