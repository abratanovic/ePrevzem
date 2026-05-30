using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AddEmployeePasswordAndRefreshTokenSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<Guid>(
                name: "employee_account_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    must_change_password = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_from_provisioning_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    roles = table.Column<string>(type: "text", nullable: false),
                    station_access = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employee_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    device_fingerprint = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    provisioned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    employee_account_id1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_devices_employee_accounts_employee_account_id1",
                        column: x => x.employee_account_id1,
                        principalTable: "employee_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_employee_account_id",
                table: "refresh_tokens",
                column: "employee_account_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens",
                sql: "(system_admin_id IS NOT NULL AND organization_admin_account_id IS NULL AND employee_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NOT NULL AND employee_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NULL AND employee_account_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_employee_accounts_email",
                table: "employee_accounts",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employee_devices_employee_account_id1",
                table: "employee_devices",
                column: "employee_account_id1");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_employee_accounts_employee_account_id",
                table: "refresh_tokens",
                column: "employee_account_id",
                principalTable: "employee_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_employee_accounts_employee_account_id",
                table: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "employee_devices");

            migrationBuilder.DropTable(
                name: "employee_accounts");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_employee_account_id",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "employee_account_id",
                table: "refresh_tokens");

            migrationBuilder.AddCheckConstraint(
                name: "CK_refresh_tokens_single_actor",
                table: "refresh_tokens",
                sql: "(system_admin_id IS NOT NULL AND organization_admin_account_id IS NULL) OR (system_admin_id IS NULL AND organization_admin_account_id IS NOT NULL)");
        }
    }
}
