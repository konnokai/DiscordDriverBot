using Microsoft.EntityFrameworkCore.Migrations;

namespace Discord_Driver_Bot.Migrations
{
    public partial class AddDbEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookData",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    URL = table.Column<string>(nullable: true),
                    DateTime = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    ExtensionData = table.Column<string>(nullable: true),
                    ThumbnailUrl = table.Column<string>(nullable: true),
                    Tags = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BotConfig",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagTranslationCreatedAt = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildInfo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    BookReadedCount = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrustedGuild",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedGuild", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateGuildInfo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelTimeId = table.Column<ulong>(nullable: false),
                    ChannelMemberId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateGuildInfo", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookData");

            migrationBuilder.DropTable(
                name: "BotConfig");

            migrationBuilder.DropTable(
                name: "GuildInfo");

            migrationBuilder.DropTable(
                name: "TrustedGuild");

            migrationBuilder.DropTable(
                name: "UpdateGuildInfo");
        }
    }
}
