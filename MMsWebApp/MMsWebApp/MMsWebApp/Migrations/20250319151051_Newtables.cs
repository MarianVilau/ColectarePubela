using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMsWebApp.Migrations
{
    /// <inheritdoc />
    public partial class Newtables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cetateni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nume = table.Column<string>(type: "TEXT", nullable: false),
                    Prenume = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    CNP = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cetateni", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pubele",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pubele", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PubeleCetateni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PubelaId = table.Column<string>(type: "TEXT", nullable: false),
                    CetateanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Adresa = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PubeleCetateni", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cetateni");

            migrationBuilder.DropTable(
                name: "Pubele");

            migrationBuilder.DropTable(
                name: "PubeleCetateni");
        }
    }
}
