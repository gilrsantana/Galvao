using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galvao.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMemberContactForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop old foreign key if it exists
            migrationBuilder.Sql(@"
                CREATE PROCEDURE temp_drop_fk_id()
                BEGIN
                    DECLARE cnt INT;
                    SELECT COUNT(*) INTO cnt 
                    FROM information_schema.table_constraints 
                    WHERE constraint_name = 'FK_MemberContacts_Members_Id' 
                      AND table_name = 'MemberContacts'
                      AND table_schema = DATABASE();
                    IF cnt > 0 THEN
                        ALTER TABLE `MemberContacts` DROP FOREIGN KEY `FK_MemberContacts_Members_Id`;
                    END IF;
                END;
            ");
            migrationBuilder.Sql("CALL temp_drop_fk_id();");
            migrationBuilder.Sql("DROP PROCEDURE temp_drop_fk_id;");

            // 2. Sync MemberId using Email match from Members table for empty MemberIds:
            migrationBuilder.Sql(@"
                UPDATE `MemberContacts` mc
                INNER JOIN `Members` m ON mc.Email = m.Email
                SET mc.MemberId = m.Id
                WHERE mc.MemberId = '00000000-0000-0000-0000-000000000000';
            ");

            // 3. Delete any remaining orphan MemberContacts that do not exist in Members:
            migrationBuilder.Sql(@"
                DELETE FROM `MemberContacts` 
                WHERE `MemberId` = '00000000-0000-0000-0000-000000000000' 
                   OR `MemberId` NOT IN (SELECT `Id` FROM `Members`);
            ");

            // 4. Create the unique index if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE PROCEDURE temp_create_idx_member_id()
                BEGIN
                    DECLARE cnt INT;
                    SELECT COUNT(*) INTO cnt 
                    FROM information_schema.statistics 
                    WHERE index_name = 'IX_MemberContacts_MemberId' 
                      AND table_name = 'MemberContacts'
                      AND table_schema = DATABASE();
                    IF cnt = 0 THEN
                        CREATE UNIQUE INDEX `IX_MemberContacts_MemberId` ON `MemberContacts` (`MemberId`);
                    END IF;
                END;
            ");
            migrationBuilder.Sql("CALL temp_create_idx_member_id();");
            migrationBuilder.Sql("DROP PROCEDURE temp_create_idx_member_id;");

            // 5. Add new foreign key constraint if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE PROCEDURE temp_add_fk_member_id()
                BEGIN
                    DECLARE cnt INT;
                    SELECT COUNT(*) INTO cnt 
                    FROM information_schema.table_constraints 
                    WHERE constraint_name = 'FK_MemberContacts_Members_MemberId' 
                      AND table_name = 'MemberContacts'
                      AND table_schema = DATABASE();
                    IF cnt = 0 THEN
                        ALTER TABLE `MemberContacts` ADD CONSTRAINT `FK_MemberContacts_Members_MemberId` FOREIGN KEY (`MemberId`) REFERENCES `Members` (`Id`) ON DELETE CASCADE;
                    END IF;
                END;
            ");
            migrationBuilder.Sql("CALL temp_add_fk_member_id();");
            migrationBuilder.Sql("DROP PROCEDURE temp_add_fk_member_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `MemberContacts` DROP FOREIGN KEY `FK_MemberContacts_Members_MemberId`;");

            migrationBuilder.DropIndex(
                name: "IX_MemberContacts_MemberId",
                table: "MemberContacts");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberContacts_Members_Id",
                table: "MemberContacts",
                column: "Id",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
