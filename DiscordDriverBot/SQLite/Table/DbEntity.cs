using System.ComponentModel.DataAnnotations;

namespace DiscordDriverBot.SQLite.Table
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
