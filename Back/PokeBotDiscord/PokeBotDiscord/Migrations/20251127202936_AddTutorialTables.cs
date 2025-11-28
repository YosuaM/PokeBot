using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TutorialSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleKey = table.Column<string>(type: "TEXT", nullable: false),
                    IntroKey = table.Column<string>(type: "TEXT", nullable: false),
                    RewardMoney = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardItemTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    RewardItemQuantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialSteps_ItemTypes_RewardItemTypeId",
                        column: x => x.RewardItemTypeId,
                        principalTable: "ItemTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerTutorialStepProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    TutorialStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardClaimed = table.Column<bool>(type: "INTEGER", nullable: false),
                    RewardClaimedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTutorialStepProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTutorialStepProgresses_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTutorialStepProgresses_TutorialSteps_TutorialStepId",
                        column: x => x.TutorialStepId,
                        principalTable: "TutorialSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutorialMissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TutorialStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DescriptionKey = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialMissions_TutorialSteps_TutorialStepId",
                        column: x => x.TutorialStepId,
                        principalTable: "TutorialSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTutorialMissionProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    TutorialMissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Completed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTutorialMissionProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTutorialMissionProgresses_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTutorialMissionProgresses_TutorialMissions_TutorialMissionId",
                        column: x => x.TutorialMissionId,
                        principalTable: "TutorialMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTutorialMissionProgresses_PlayerId_TutorialMissionId",
                table: "PlayerTutorialMissionProgresses",
                columns: new[] { "PlayerId", "TutorialMissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTutorialMissionProgresses_TutorialMissionId",
                table: "PlayerTutorialMissionProgresses",
                column: "TutorialMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTutorialStepProgresses_PlayerId_TutorialStepId",
                table: "PlayerTutorialStepProgresses",
                columns: new[] { "PlayerId", "TutorialStepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTutorialStepProgresses_TutorialStepId",
                table: "PlayerTutorialStepProgresses",
                column: "TutorialStepId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialMissions_TutorialStepId",
                table: "TutorialMissions",
                column: "TutorialStepId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialSteps_RewardItemTypeId",
                table: "TutorialSteps",
                column: "RewardItemTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerTutorialMissionProgresses");

            migrationBuilder.DropTable(
                name: "PlayerTutorialStepProgresses");

            migrationBuilder.DropTable(
                name: "TutorialMissions");

            migrationBuilder.DropTable(
                name: "TutorialSteps");
        }
    }
}
