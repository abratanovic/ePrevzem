using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AddDeviceChallengeAndCitizenRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<Guid>(
                name: "citizen_user_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "device_challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_kind = table.Column<int>(type: "integer", nullable: false),
                    nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_challenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_citizen_user_id",
                table: "refresh_tokens",
                column: "citizen_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens",
                sql: "(CASE WHEN system_admin_id IS NULL THEN 0 ELSE 1 END + CASE WHEN organization_admin_account_id IS NULL THEN 0 ELSE 1 END + CASE WHEN employee_account_id IS NULL THEN 0 ELSE 1 END + CASE WHEN citizen_user_id IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_device_challenges_device_id_consumed_at",
                table: "device_challenges",
                columns: new[] { "device_id", "consumed_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_citizen_users_citizen_user_id",
                table: "refresh_tokens",
                column: "citizen_user_id",
                principalTable: "citizen_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_citizen_users_citizen_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "device_challenges");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_citizen_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "citizen_user_id",
                table: "refresh_tokens");

            migrationBuilder.AddCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens",
                sql: "(system_admin_id IS NOT NULL AND organization_admin_account_id IS NULL AND employee_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NOT NULL AND employee_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NULL AND employee_account_id IS NOT NULL)");
        }
    }
}
