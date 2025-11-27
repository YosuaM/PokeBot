using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddMoveEventConfigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MoveBattleEventChancePercent",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MoveBattleWinMaxMoneyReward",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MoveBattleWinMinMoneyReward",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MoveItemEventChancePercent",
                table: "GuildSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MoveRandomItemRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveRandomItemRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoveRandomItemRewards_ItemTypes_ItemTypeId",
                        column: x => x.ItemTypeId,
                        principalTable: "ItemTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MoveRandomItemRewards_ItemTypeId",
                table: "MoveRandomItemRewards",
                column: "ItemTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MoveRandomItemRewards");

            migrationBuilder.DropColumn(
                name: "MoveBattleEventChancePercent",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "MoveBattleWinMaxMoneyReward",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "MoveBattleWinMinMoneyReward",
                table: "GuildSettings");

            migrationBuilder.DropColumn(
                name: "MoveItemEventChancePercent",
                table: "GuildSettings");
        }
    }
}
