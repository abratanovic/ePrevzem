using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AddOrgAdminRefreshTokenSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_system_admin_id_revoked_at",
                table: "refresh_tokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "system_admin_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "organization_admin_account_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_organization_admin_account_id",
                table: "refresh_tokens",
                column: "organization_admin_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_system_admin_id",
                table: "refresh_tokens",
                column: "system_admin_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens",
                sql: "(system_admin_id IS NOT NULL AND organization_admin_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_organization_admin_accounts_organization_adm~",
                table: "refresh_tokens",
                column: "organization_admin_account_id",
                principalTable: "organization_admin_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_organization_admin_accounts_organization_adm~",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_organization_admin_account_id",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_system_admin_id",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "organization_admin_account_id",
                table: "refresh_tokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "system_admin_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_system_admin_id_revoked_at",
                table: "refresh_tokens",
                columns: new[] { "system_admin_id", "revoked_at" });
        }
    }
}
