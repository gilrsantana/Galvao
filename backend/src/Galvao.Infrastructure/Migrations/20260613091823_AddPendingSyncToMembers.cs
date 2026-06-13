using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galvao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSyncToMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PendingSync",
                table: "Members",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingSync",
                table: "Members");
        }
    }
}
