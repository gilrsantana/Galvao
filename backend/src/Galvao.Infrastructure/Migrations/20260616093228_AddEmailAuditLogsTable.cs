using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galvao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAuditLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    MemberId = table.Column<Guid>(type: "char(36)", nullable: true),
                    RecipientEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    EMailProvider = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ETypeOfMessage = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAuditLogs_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAuditLogs_MemberId",
                table: "EmailAuditLogs",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAuditLogs");
        }
    }
}
