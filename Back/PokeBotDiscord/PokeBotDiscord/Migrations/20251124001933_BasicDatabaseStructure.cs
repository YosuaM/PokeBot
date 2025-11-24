using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class BasicDatabaseStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gyms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PokemonSpecies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonSpecies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasWildEncounters = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessToShop = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessToGym = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessToPokemonCenter = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessToPokemonLeague = table.Column<bool>(type: "INTEGER", nullable: false),
                    GymId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationTypes_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LocationTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_LocationTypes_LocationTypeId",
                        column: x => x.LocationTypeId,
                        principalTable: "LocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CurrentLocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Money = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTurnAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TurnCredits = table.Column<decimal>(type: "decimal(18,1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Locations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_ItemTypes_ItemTypeId",
                        column: x => x.ItemTypeId,
                        principalTable: "ItemTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PokemonInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PokemonSpeciesId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<long>(type: "INTEGER", nullable: false),
                    PlayerId1 = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PokemonInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PokemonInstances_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PokemonInstances_Players_PlayerId1",
                        column: x => x.PlayerId1,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PokemonInstances_PokemonSpecies_PokemonSpeciesId",
                        column: x => x.PokemonSpeciesId,
                        principalTable: "PokemonSpecies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ItemTypeId",
                table: "InventoryItems",
                column: "ItemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_PlayerId",
                table: "InventoryItems",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationTypeId",
                table: "Locations",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_GymId",
                table: "LocationTypes",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_CurrentLocationId",
                table: "Players",
                column: "CurrentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GuildId_DiscordUserId",
                table: "Players",
                columns: new[] { "GuildId", "DiscordUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonInstances_PlayerId",
                table: "PokemonInstances",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonInstances_PlayerId1",
                table: "PokemonInstances",
                column: "PlayerId1");

            migrationBuilder.CreateIndex(
                name: "IX_PokemonInstances_PokemonSpeciesId",
                table: "PokemonInstances",
                column: "PokemonSpeciesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "PokemonInstances");

            migrationBuilder.DropTable(
                name: "ItemTypes");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "PokemonSpecies");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "LocationTypes");

            migrationBuilder.DropTable(
                name: "Gyms");
        }
    }
}
