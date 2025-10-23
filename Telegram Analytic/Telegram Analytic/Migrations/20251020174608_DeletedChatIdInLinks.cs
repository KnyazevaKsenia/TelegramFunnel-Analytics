using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram_Analytic.Migrations
{
    /// <inheritdoc />
    public partial class DeletedChatIdInLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TelegramChatId",
                table: "Projects",
                newName: "TelegramChanelUsername");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TelegramChanelUsername",
                table: "Projects",
                newName: "TelegramChatId");
        }
    }
}
