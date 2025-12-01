using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telegram_Analytic.Migrations
{
    /// <inheritdoc />
    public partial class ProjectStructureChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramBotToken",
                table: "Projects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelegramBotToken",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
