using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonEncounterMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EncounterMethodId",
                table: "PokemonEncounters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "GymId",
                table: "GymTrainers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncounterMethodId",
                table: "PokemonEncounters");

            migrationBuilder.AlterColumn<int>(
                name: "GymId",
                table: "GymTrainers",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
