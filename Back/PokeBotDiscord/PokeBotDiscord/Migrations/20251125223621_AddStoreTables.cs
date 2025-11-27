using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationStores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationStores_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationStores_StoreTypes_StoreTypeId",
                        column: x => x.StoreTypeId,
                        principalTable: "StoreTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreTypeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StoreTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTypeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreTypeItems_ItemTypes_ItemTypeId",
                        column: x => x.ItemTypeId,
                        principalTable: "ItemTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreTypeItems_StoreTypes_StoreTypeId",
                        column: x => x.StoreTypeId,
                        principalTable: "StoreTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationStores_LocationId_StoreTypeId",
                table: "LocationStores",
                columns: new[] { "LocationId", "StoreTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationStores_StoreTypeId",
                table: "LocationStores",
                column: "StoreTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreTypeItems_ItemTypeId",
                table: "StoreTypeItems",
                column: "ItemTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreTypeItems_StoreTypeId_ItemTypeId",
                table: "StoreTypeItems",
                columns: new[] { "StoreTypeId", "ItemTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationStores");

            migrationBuilder.DropTable(
                name: "StoreTypeItems");

            migrationBuilder.DropTable(
                name: "StoreTypes");
        }
    }
}
