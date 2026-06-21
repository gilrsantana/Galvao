using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galvao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnsubscribedAndAddEmailSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unsubscribed",
                table: "MemberContacts");

            migrationBuilder.AddColumn<string>(
                name: "EmailProvider",
                table: "MemberContacts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId",
                table: "MemberContacts",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "MemberContacts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    MemberContactId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ESegmentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SubscriptionDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UnSubscriptionDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSegments_MemberContacts_MemberContactId",
                        column: x => x.MemberContactId,
                        principalTable: "MemberContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSegments_MemberContactId",
                table: "EmailSegments",
                column: "MemberContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSegments");

            migrationBuilder.DropColumn(
                name: "EmailProvider",
                table: "MemberContacts");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "MemberContacts");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "MemberContacts");

            migrationBuilder.AddColumn<bool>(
                name: "Unsubscribed",
                table: "MemberContacts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
