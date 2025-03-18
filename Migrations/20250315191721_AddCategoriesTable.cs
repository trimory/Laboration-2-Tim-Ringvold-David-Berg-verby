using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laboration2_MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "CategoryRules");

            migrationBuilder.AddColumn<int>(
                name: "CategoryID",
                table: "CategoryRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryRules_CategoryID",
                table: "CategoryRules",
                column: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryRules_Categories_CategoryID",
                table: "CategoryRules",
                column: "CategoryID",
                principalTable: "Categories",
                principalColumn: "CategoryID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryRules_Categories_CategoryID",
                table: "CategoryRules");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_CategoryRules_CategoryID",
                table: "CategoryRules");

            migrationBuilder.DropColumn(
                name: "CategoryID",
                table: "CategoryRules");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CategoryRules",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
