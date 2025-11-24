using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePokemonEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PokemonEncounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PokemonSpeciesId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    MinLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonEncounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonEncounters_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonEncounters_PokemonSpecies_PokemonSpeciesId",
                        column: x => x.PokemonSpeciesId,
                        principalTable: "PokemonSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokemonEncounters_LocationId",
                table: "PokemonEncounters",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonEncounters_PokemonSpeciesId",
                table: "PokemonEncounters",
                column: "PokemonSpeciesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokemonEncounters");
        }
    }
}
