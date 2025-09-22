using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LM01_UI.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeSystemKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SystemKey",
                table: "Recipes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_SystemKey",
                table: "Recipes",
                column: "SystemKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_SystemKey",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "Recipes");
        }
    }
}