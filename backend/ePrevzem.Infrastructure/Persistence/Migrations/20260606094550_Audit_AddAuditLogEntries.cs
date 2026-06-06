using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Audit_AddAuditLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_kind = table.Column<string>(type: "text", nullable: false),
                    actor_citizen_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_employee_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_organization_admin_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_system_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    target_kind = table.Column<string>(type: "text", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log_entries", x => x.Id);
                    table.CheckConstraint("ck_audit_log_entries_actor", "(\n    actor_kind = 'System'\n    AND actor_citizen_user_id IS NULL\n    AND actor_employee_account_id IS NULL\n    AND actor_organization_admin_account_id IS NULL\n    AND actor_system_admin_id IS NULL\n)\nOR (\n    actor_kind = 'Citizen'\n    AND actor_citizen_user_id IS NOT NULL\n    AND actor_employee_account_id IS NULL\n    AND actor_organization_admin_account_id IS NULL\n    AND actor_system_admin_id IS NULL\n)\nOR (\n    actor_kind = 'Employee'\n    AND actor_citizen_user_id IS NULL\n    AND actor_employee_account_id IS NOT NULL\n    AND actor_organization_admin_account_id IS NULL\n    AND actor_system_admin_id IS NULL\n)\nOR (\n    actor_kind = 'OrganizationAdmin'\n    AND actor_citizen_user_id IS NULL\n    AND actor_employee_account_id IS NULL\n    AND actor_organization_admin_account_id IS NOT NULL\n    AND actor_system_admin_id IS NULL\n)\nOR (\n    actor_kind = 'SystemAdmin'\n    AND actor_citizen_user_id IS NULL\n    AND actor_employee_account_id IS NULL\n    AND actor_organization_admin_account_id IS NULL\n    AND actor_system_admin_id IS NOT NULL\n)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_action",
                table: "audit_log_entries",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_actor_citizen_user_id",
                table: "audit_log_entries",
                column: "actor_citizen_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_actor_employee_account_id",
                table: "audit_log_entries",
                column: "actor_employee_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_actor_organization_admin_account_id",
                table: "audit_log_entries",
                column: "actor_organization_admin_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_actor_system_admin_id",
                table: "audit_log_entries",
                column: "actor_system_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_organization_id_occurred_at",
                table: "audit_log_entries",
                columns: new[] { "organization_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_target_kind_target_id",
                table: "audit_log_entries",
                columns: new[] { "target_kind", "target_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log_entries");
        }
    }
}
