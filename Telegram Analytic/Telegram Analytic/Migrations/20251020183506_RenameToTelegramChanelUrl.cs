using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram_Analytic.Migrations
{
    /// <inheritdoc />
    public partial class RenameToTelegramChanelUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TelegramChanelUsername",
                table: "Projects",
                newName: "TelegramChanelUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TelegramChanelUrl",
                table: "Projects",
                newName: "TelegramChanelUsername");
        }
    }
}
