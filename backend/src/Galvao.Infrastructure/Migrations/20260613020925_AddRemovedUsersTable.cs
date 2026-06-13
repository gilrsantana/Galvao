using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galvao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemovedUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemovedUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    RemovedPersonalInformation = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RemovedAccountData = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RemovedMarketData = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RemovedFromMailProvider = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemovedUsers", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemovedUsers");
        }
    }
}
