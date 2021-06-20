namespace Discord_Driver_Bot.SQLite.Table
{
    class UpdateGuildInfo : DbEntity
    {
        public ulong GuildId { get; set; }

        public ulong ChannelTimeId { get; set; }

        public ulong ChannelMemberId { get; set; }
    }
}
