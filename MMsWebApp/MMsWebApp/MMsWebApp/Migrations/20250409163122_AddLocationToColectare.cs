using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMsWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToColectare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adresa",
                table: "Colectari",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Colectari",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Colectari",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adresa",
                table: "Colectari");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Colectari");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Colectari");
        }
    }
}
