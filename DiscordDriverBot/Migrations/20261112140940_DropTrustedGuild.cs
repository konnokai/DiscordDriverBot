using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordDriverBot.Migrations
{
    /// <inheritdoc />
    public partial class DropTrustedGuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TrustedGuild 是 EnsureCreated 時期留下的表，程式已經沒在用
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"TrustedGuild\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrustedGuild",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedGuild", x => x.Id);
                });
        }
    }
}
