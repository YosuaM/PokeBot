using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeCode",
                table: "Gyms",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocationConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredGymId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationConnections_Gyms_RequiredGymId",
                        column: x => x.RequiredGymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationConnections_Locations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationConnections_Locations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGymBadges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    GymId = table.Column<int>(type: "INTEGER", nullable: false),
                    ObtainedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGymBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerGymBadges_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerGymBadges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationConnections_FromLocationId",
                table: "LocationConnections",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationConnections_RequiredGymId",
                table: "LocationConnections",
                column: "RequiredGymId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationConnections_ToLocationId",
                table: "LocationConnections",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGymBadges_GymId",
                table: "PlayerGymBadges",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGymBadges_PlayerId_GymId",
                table: "PlayerGymBadges",
                columns: new[] { "PlayerId", "GymId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationConnections");

            migrationBuilder.DropTable(
                name: "PlayerGymBadges");

            migrationBuilder.DropColumn(
                name: "BadgeCode",
                table: "Gyms");
        }
    }
}
