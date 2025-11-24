using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddStamina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TurnCredits",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "CurrentStamina",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxStamina",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StaminaPerHour",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStamina",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MaxStamina",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "StaminaPerHour",
                table: "GuildSettings");

            migrationBuilder.AddColumn<decimal>(
                name: "TurnCredits",
                table: "Players",
                type: "decimal(18,1)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
