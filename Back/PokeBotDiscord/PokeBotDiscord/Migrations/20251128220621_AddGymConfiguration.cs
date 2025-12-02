using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class AddGymConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpen",
                table: "Gyms",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Gyms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GymTrainers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GymId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Pokemon1SpeciesId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon1Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon2SpeciesId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon2Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon3SpeciesId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon3Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon4SpeciesId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon4Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon5SpeciesId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon5Level = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon6SpeciesId = table.Column<int>(type: "INTEGER", nullable: true),
                    Pokemon6Level = table.Column<int>(type: "INTEGER", nullable: true),
                    RewardMoney = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardItemTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    RewardItemQuantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymTrainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GymTrainers_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GymTrainers_ItemTypes_RewardItemTypeId",
                        column: x => x.RewardItemTypeId,
                        principalTable: "ItemTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gyms_LocationId",
                table: "Gyms",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GymTrainers_GymId",
                table: "GymTrainers",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_GymTrainers_RewardItemTypeId",
                table: "GymTrainers",
                column: "RewardItemTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Gyms_Locations_LocationId",
                table: "Gyms",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gyms_Locations_LocationId",
                table: "Gyms");

            migrationBuilder.DropTable(
                name: "GymTrainers");

            migrationBuilder.DropIndex(
                name: "IX_Gyms_LocationId",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "IsOpen",
                table: "Gyms");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Gyms");
        }
    }
}
