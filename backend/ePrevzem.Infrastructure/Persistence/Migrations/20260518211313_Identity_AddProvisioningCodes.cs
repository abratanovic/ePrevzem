using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AddProvisioningCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provisioning_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    pre_filled_first_name = table.Column<string>(type: "text", nullable: false),
                    pre_filled_last_name = table.Column<string>(type: "text", nullable: false),
                    pre_filled_email = table.Column<string>(type: "text", nullable: true),
                    roles = table.Column<string>(type: "text", nullable: false),
                    station_access = table.Column<string>(type: "text", nullable: false),
                    created_by_organization_admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    redeemed_into_employee_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_reprovisioning_of_employee_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provisioning_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provisioning_codes_organization_admin_accounts_created_by_o~",
                        column: x => x.created_by_organization_admin_id,
                        principalTable: "organization_admin_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_codes_code",
                table: "provisioning_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provisioning_codes_created_by_organization_admin_id",
                table: "provisioning_codes",
                column: "created_by_organization_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provisioning_codes");
        }
    }
}
