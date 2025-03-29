using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMsWebApp.Migrations
{
    /// <inheritdoc />
    public partial class CleanupData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PubelaId",
                table: "PubeleCetateni",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Pubele",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.CreateIndex(
                name: "IX_PubeleCetateni_CetateanId",
                table: "PubeleCetateni",
                column: "CetateanId");

            migrationBuilder.CreateIndex(
                name: "IX_PubeleCetateni_PubelaId",
                table: "PubeleCetateni",
                column: "PubelaId");

            migrationBuilder.CreateIndex(
                name: "IX_Colectari_IdPubela",
                table: "Colectari",
                column: "IdPubela");

            migrationBuilder.AddForeignKey(
                name: "FK_Colectari_Pubele_IdPubela",
                table: "Colectari",
                column: "IdPubela",
                principalTable: "Pubele",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PubeleCetateni_Cetateni_CetateanId",
                table: "PubeleCetateni",
                column: "CetateanId",
                principalTable: "Cetateni",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PubeleCetateni_Pubele_PubelaId",
                table: "PubeleCetateni",
                column: "PubelaId",
                principalTable: "Pubele",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colectari_Pubele_IdPubela",
                table: "Colectari");

            migrationBuilder.DropForeignKey(
                name: "FK_PubeleCetateni_Cetateni_CetateanId",
                table: "PubeleCetateni");

            migrationBuilder.DropForeignKey(
                name: "FK_PubeleCetateni_Pubele_PubelaId",
                table: "PubeleCetateni");

            migrationBuilder.DropIndex(
                name: "IX_PubeleCetateni_CetateanId",
                table: "PubeleCetateni");

            migrationBuilder.DropIndex(
                name: "IX_PubeleCetateni_PubelaId",
                table: "PubeleCetateni");

            migrationBuilder.DropIndex(
                name: "IX_Colectari_IdPubela",
                table: "Colectari");

            migrationBuilder.AlterColumn<int>(
                name: "PubelaId",
                table: "PubeleCetateni",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Pubele",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);
        }
    }
}
