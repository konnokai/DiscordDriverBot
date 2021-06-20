using Discord_Driver_Bot.SQLite.Table;
using System.Linq;

namespace Discord_Driver_Bot.SQLite
{
    class SQLiteFunction
    {
        public static void UpdateGuildReadedBook(ulong guildId)
        {
            using (var db = new DriverContext())
            {
                var guild = db.GuildInfo.FirstOrDefault((x) => x.GuildId == guildId);
                if (guild == null)
                {
                    db.GuildInfo.Add(new GuildInfo() { GuildId = guildId, BookReadedCount = 1 });
                }
                else
                {
                    guild.BookReadedCount += 1;
                    db.GuildInfo.Update(guild);
                }
                db.SaveChanges();
            }
        }

        public static bool GetBookData(string url, out BookData bookData)
        {
            if (Program.ListBookLogData.Any((x) => x.URL == url))
            {
                bookData = Program.ListBookLogData.Find((x) => x.URL == url);
                return true;
            }
            else
            {
                bookData = null;
                return false;
            }
        }
    }
}
