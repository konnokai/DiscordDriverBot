using Discord_Driver_Bot.SQLite.Table;
using Microsoft.EntityFrameworkCore;

namespace Discord_Driver_Bot.SQLite
{
    class DriverContext : DbContext
    {
        public DbSet<DbBotConfig> DbBotConfig { get; set; }
        public DbSet<BookData> BookData { get; set; }
        public DbSet<GuildInfo> GuildInfo { get; set; }
        public DbSet<TrustedGuild> TrustedGuild { get; set; }
        public DbSet<UpdateGuildInfo> UpdateGuildInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={Program.GetDataFilePath("DataBase.db")}");
    }
}
