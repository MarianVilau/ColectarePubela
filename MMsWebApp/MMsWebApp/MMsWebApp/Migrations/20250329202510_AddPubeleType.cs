using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMsWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPubeleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tip",
                table: "Pubele",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PubelaId",
                table: "Colectari",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colectari_PubelaId",
                table: "Colectari",
                column: "PubelaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colectari_Pubele_PubelaId",
                table: "Colectari",
                column: "PubelaId",
                principalTable: "Pubele",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colectari_Pubele_PubelaId",
                table: "Colectari");

            migrationBuilder.DropIndex(
                name: "IX_Colectari_PubelaId",
                table: "Colectari");

            migrationBuilder.DropColumn(
                name: "Tip",
                table: "Pubele");

            migrationBuilder.DropColumn(
                name: "PubelaId",
                table: "Colectari");
        }
    }
}
