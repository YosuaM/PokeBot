using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddGymPlayerProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerGymTrainerProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    GymTrainerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Defeated = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstDefeatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGymTrainerProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerGymTrainerProgresses_GymTrainers_GymTrainerId",
                        column: x => x.GymTrainerId,
                        principalTable: "GymTrainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerGymTrainerProgresses_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGymTrainerProgresses_GymTrainerId",
                table: "PlayerGymTrainerProgresses",
                column: "GymTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGymTrainerProgresses_PlayerId_GymTrainerId",
                table: "PlayerGymTrainerProgresses",
                columns: new[] { "PlayerId", "GymTrainerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGymTrainerProgresses");
        }
    }
}
