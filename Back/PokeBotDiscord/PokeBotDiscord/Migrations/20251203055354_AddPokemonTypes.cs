using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PokemonTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PokemonSpeciesTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PokemonSpeciesId = table.Column<int>(type: "INTEGER", nullable: false),
                    PokemonTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonSpeciesTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonSpeciesTypes_PokemonSpecies_PokemonSpeciesId",
                        column: x => x.PokemonSpeciesId,
                        principalTable: "PokemonSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonSpeciesTypes_PokemonTypes_PokemonTypeId",
                        column: x => x.PokemonTypeId,
                        principalTable: "PokemonTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PokemonTypeEffectiveness",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttackerTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DefenderTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Multiplier = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonTypeEffectiveness", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonTypeEffectiveness_PokemonTypes_AttackerTypeId",
                        column: x => x.AttackerTypeId,
                        principalTable: "PokemonTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonTypeEffectiveness_PokemonTypes_DefenderTypeId",
                        column: x => x.DefenderTypeId,
                        principalTable: "PokemonTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokemonSpeciesTypes_PokemonSpeciesId_PokemonTypeId",
                table: "PokemonSpeciesTypes",
                columns: new[] { "PokemonSpeciesId", "PokemonTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonSpeciesTypes_PokemonTypeId",
                table: "PokemonSpeciesTypes",
                column: "PokemonTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonTypeEffectiveness_AttackerTypeId_DefenderTypeId",
                table: "PokemonTypeEffectiveness",
                columns: new[] { "AttackerTypeId", "DefenderTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonTypeEffectiveness_DefenderTypeId",
                table: "PokemonTypeEffectiveness",
                column: "DefenderTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PokemonSpeciesTypes");

            migrationBuilder.DropTable(
                name: "PokemonTypeEffectiveness");

            migrationBuilder.DropTable(
                name: "PokemonTypes");
        }
    }
}
