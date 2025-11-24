using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "LocationTypes");

            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "LocationTypes");

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Locations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "Locations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "Locations");

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "LocationTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "LocationTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
