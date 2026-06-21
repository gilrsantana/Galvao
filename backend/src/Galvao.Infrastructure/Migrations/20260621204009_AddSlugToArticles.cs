using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galvao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugToArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Articles",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Articles SET Slug = CONCAT('article-', LOWER(SUBSTRING(Id, 1, 8)));");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Slug",
                table: "Articles",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_Slug",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Articles");
        }
    }
}
