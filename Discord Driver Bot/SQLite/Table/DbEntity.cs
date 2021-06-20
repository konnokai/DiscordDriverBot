using System.ComponentModel.DataAnnotations;

namespace Discord_Driver_Bot.SQLite.Table
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
