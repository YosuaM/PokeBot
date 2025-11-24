using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeBotDiscord.Migrations
{
    /// <inheritdoc />
    public partial class RemovePokemonStorageCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PokemonInstances_Players_PlayerId1",
                table: "PokemonInstances");

            migrationBuilder.DropIndex(
                name: "IX_PokemonInstances_PlayerId1",
                table: "PokemonInstances");

            migrationBuilder.DropColumn(
                name: "PlayerId1",
                table: "PokemonInstances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PlayerId1",
                table: "PokemonInstances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PokemonInstances_PlayerId1",
                table: "PokemonInstances",
                column: "PlayerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PokemonInstances_Players_PlayerId1",
                table: "PokemonInstances",
                column: "PlayerId1",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
