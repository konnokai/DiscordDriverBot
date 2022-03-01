using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord_Driver_Bot.Command.Administration
{
    public class Administration : TopLevelModule<AdministrationService>
    {
        private readonly AdministrationService _admin;
        private readonly DiscordSocketClient _client;
        public Administration(AdministrationService administraionServer, DiscordSocketClient discordSocketClient)
        {
            _admin = administraionServer;
            _client = discordSocketClient;
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Command("Clear")]
        [Summary("清除指定成員所請求的本子")]
        [Priority(0)]
        public async Task Clear([Summary("使用者UID")] ulong uid)
        {
            if ((await Context.Guild.GetUserAsync(uid)) != null)
            {
                await _admin.ClearUser((ITextChannel)Context.Channel, uid);
                await Context.Channel.SendConfirmAsync("已清除");
            }
            else await Context.Channel.SendErrorAsync("找不到指定的成員");
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Command("Clear")]
        [Summary("清除指定成員所請求的本子")]
        [Priority(1)]
        public Task Clear([Summary("@使用者")] IUser user) => Clear(user.Id);

        [RequireContext(ContextType.Guild)]
        [RequireOwner]
        [Command("Clear")]
        [Summary("清除機器人的發言")]
        public async Task Clear()
        {
            await _admin.ClearUser((ITextChannel)Context.Channel);
        }

        [Command("UpdateStatus")]
        [Summary("更新機器人的狀態\n參數: Guild, Member, ShowBook, Info, ReadBook")]
        [Alias("UpStats")]
        [RequireOwner]
        public async Task UpdateStatusAsync([Summary("狀態")] string stats)
        {
            switch (stats.ToLowerInvariant())
            {
                case "guild":
                    Program.updateStatus = Program.UpdateStatus.Guild;
                    break;
                case "member":
                    Program.updateStatus = Program.UpdateStatus.Member;
                    break;
                case "showbook":
                    Program.updateStatus = Program.UpdateStatus.ShowBook;
                    break;
                case "info":
                    Program.updateStatus = Program.UpdateStatus.Info;
                    break;
                case "readbook":
                    Program.updateStatus = Program.UpdateStatus.ReadBook;
                    break;
                default:
                    await Context.Channel.SendErrorAsync(string.Format("找不到 {0} 狀態", stats));
                    return;
            }
            Program.ChangeStatus();
            return;
        }

        [Command("Say")]
        [Summary("說話")]
        [RequireOwner]
        public async Task SayAsync([Summary("內容")][Remainder] string text)
        {
            await ReplyAsync(text);
        }

        [Command("ListServer")]
        [Summary("顯示所有的伺服器")]
        [Alias("LS")]
        [RequireOwner]
        public async Task ListServerAsync([Summary("頁數")] int page = 0)
        {
            await Context.SendPaginatedConfirmAsync(page, (cur) =>
            {
                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle("目前所在的伺服器有");

                foreach (var item in _client.Guilds.Skip(cur * 5).Take(5))
                {
                    int totalMember = item.Users.Count((x) => !x.IsBot);
                    bool isBotOwnerInGuild = item.GetUser(Program.ApplicatonOwner.Id) != null;
                    embedBuilder.AddField(item.Name, "ID: " + item.Id +
                        "\nOwner ID: " + item.OwnerId +
                        "\n人數: " + totalMember.ToString() +
                        "\nBot擁有者是否在該伺服器: " + (isBotOwnerInGuild ? "是" : "否"));
                }

                return embedBuilder;
            }, _client.Guilds.Count, 5);
        }

        [Command("Die")]
        [Summary("關閉機器人")]
        [Alias("Bye")]
        [RequireOwner]
        public async Task DieAsync()
        {
            Program.isDisconnect = true;
            await Context.Channel.SendConfirmAsync("關閉中");
        }

        [Command("Leave")]
        [Summary("讓機器人離開指定的伺服器")]
        [RequireOwner]
        public async Task LeaveAsync([Summary("伺服器ID")] ulong gid = 0)
        {
            if (gid == 0) { await ReplyAsync("伺服器ID為空"); return; }

            try { await _client.GetGuild(gid).LeaveAsync(); }
            catch (Exception) { await Context.Channel.SendErrorAsync("失敗，請確認ID是否正確"); return; }

            await ReplyAsync("✅");
        }

        [Command("GetInviteURL")]
        [Summary("取得伺服器的邀請連結")]
        [RequireBotPermission(GuildPermission.CreateInstantInvite)]
        [RequireOwner]
        public async Task GetInviteURLAsync([Summary("伺服器ID")] ulong gid = 0, [Summary("頻道ID")] ulong cid = 0)
        {
            if (gid == 0) gid = Context.Guild.Id;
            SocketGuild guild = _client.GetGuild(gid);

            try
            {
                if (cid == 0)
                {
                    IReadOnlyCollection<SocketTextChannel> socketTextChannels = guild.TextChannels;

                    await Context.SendPaginatedConfirmAsync(0, (cur) =>
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder()
                           .WithOkColor()
                           .WithTitle("以下為 " + guild.Name + " 所有的文字頻道")
                           .WithDescription(string.Join('\n', socketTextChannels.Skip(cur * 10).Take(10).Select((x) => x.Id + " / " + x.Name)));

                        return embedBuilder;
                    }, socketTextChannels.Count, 10);
                }
                else
                {
                    IInviteMetadata invite = await guild.GetTextChannel(cid).CreateInviteAsync(300, 1, false);
                    await ReplyAsync(invite.Url);
                }
            }
            catch (Exception ex) { Log.Error(ex.ToString()); await ReplyAsync(ex.Message); }
        }

        [Command("redb")]
        [Summary("整理資料庫")]
        [RequireOwner]
        public async Task ResetDB()
        {
            using (var db = new SQLite.DriverContext())
            {
                var list = new List<SQLite.Table.BookData>();
                int old = db.BookData.Count();

                foreach (var item in db.BookData.ToList())
                {
                    if (!list.Any((x) => x.URL == item.URL))
                    {
                        list.Add(item);
                    }
                }

                foreach (var item in list)
                {
                    if (item.URL.Contains(".html"))
                        item.URL = item.URL.Split(new string[] { ".html" }, StringSplitOptions.RemoveEmptyEntries)[0] + ".html";
                }

                try
                {
                    db.Database.ExecuteSqlRaw("DELETE FROM BookData");
                    db.BookData.AddRange(list);
                    db.SaveChanges();
                    await Context.Channel.SendConfirmAsync($"{old}/{db.BookData.Count()}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}