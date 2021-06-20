namespace Discord_Driver_Bot.SQLite.Table
{
    class GuildInfo : DbEntity
    {
        public ulong GuildId { get; set; }
        public uint BookReadedCount { get; set; }
    }
}
