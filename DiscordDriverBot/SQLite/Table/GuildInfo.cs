namespace DiscordDriverBot.SQLite.Table
{
    class GuildInfo : DbEntity
    {
        public ulong GuildId { get; set; }
        public uint BookReadedCount { get; set; }
    }
}
