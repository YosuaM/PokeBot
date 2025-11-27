using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddPokemonRarity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptureRate",
                table: "PokemonSpecies");

            migrationBuilder.AddColumn<int>(
                name: "PokemonRarityId",
                table: "PokemonSpecies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PokemonRarities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    MinMoneyReward = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMoneyReward = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonRarities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PokemonRarityCatchRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PokemonRarityId = table.Column<int>(type: "INTEGER", nullable: false),
                    BallCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CatchRatePercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonRarityCatchRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonRarityCatchRates_PokemonRarities_PokemonRarityId",
                        column: x => x.PokemonRarityId,
                        principalTable: "PokemonRarities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PokemonSpecies_PokemonRarityId",
                table: "PokemonSpecies",
                column: "PokemonRarityId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonRarityCatchRates_PokemonRarityId_BallCode",
                table: "PokemonRarityCatchRates",
                columns: new[] { "PokemonRarityId", "BallCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PokemonSpecies_PokemonRarities_PokemonRarityId",
                table: "PokemonSpecies",
                column: "PokemonRarityId",
                principalTable: "PokemonRarities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PokemonSpecies_PokemonRarities_PokemonRarityId",
                table: "PokemonSpecies");

            migrationBuilder.DropTable(
                name: "PokemonRarityCatchRates");

            migrationBuilder.DropTable(
                name: "PokemonRarities");

            migrationBuilder.DropIndex(
                name: "IX_PokemonSpecies_PokemonRarityId",
                table: "PokemonSpecies");

            migrationBuilder.DropColumn(
                name: "PokemonRarityId",
                table: "PokemonSpecies");

            migrationBuilder.AddColumn<int>(
                name: "CaptureRate",
                table: "PokemonSpecies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
