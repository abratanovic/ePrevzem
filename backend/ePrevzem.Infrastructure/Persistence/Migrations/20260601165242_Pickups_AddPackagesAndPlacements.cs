using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Pickups_AddPackagesAndPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_citizen_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_employee_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_organization_admin_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_pickup_station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    deadline_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => x.Id);
                    table.CheckConstraint("ck_packages_exactly_one_creator", "(created_by_employee_account_id IS NOT NULL) <> (created_by_organization_admin_account_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_packages_citizen_users_recipient_citizen_user_id",
                        column: x => x.recipient_citizen_user_id,
                        principalTable: "citizen_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_packages_employee_accounts_created_by_employee_account_id",
                        column: x => x.created_by_employee_account_id,
                        principalTable: "employee_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_packages_organization_admin_accounts_created_by_organizatio~",
                        column: x => x.created_by_organization_admin_account_id,
                        principalTable: "organization_admin_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_packages_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_packages_pickup_stations_target_pickup_station_id",
                        column: x => x.target_pickup_station_id,
                        principalTable: "pickup_stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "placements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_by_employee_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_reason = table.Column<string>(type: "text", nullable: true),
                    ended_by_citizen_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ended_by_employee_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_placements_citizen_users_ended_by_citizen_user_id",
                        column: x => x.ended_by_citizen_user_id,
                        principalTable: "citizen_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_placements_employee_accounts_ended_by_employee_account_id",
                        column: x => x.ended_by_employee_account_id,
                        principalTable: "employee_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_placements_employee_accounts_opened_by_employee_account_id",
                        column: x => x.opened_by_employee_account_id,
                        principalTable: "employee_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_placements_lockers_locker_id",
                        column: x => x.locker_id,
                        principalTable: "lockers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_placements_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_packages_created_by_employee_account_id",
                table: "packages",
                column: "created_by_employee_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_created_by_organization_admin_account_id",
                table: "packages",
                column: "created_by_organization_admin_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_organization_id",
                table: "packages",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_recipient_citizen_user_id",
                table: "packages",
                column: "recipient_citizen_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_reference",
                table: "packages",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_packages_target_pickup_station_id",
                table: "packages",
                column: "target_pickup_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_placements_ended_by_citizen_user_id",
                table: "placements",
                column: "ended_by_citizen_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_placements_ended_by_employee_account_id",
                table: "placements",
                column: "ended_by_employee_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_placements_locker_id",
                table: "placements",
                column: "locker_id",
                unique: true,
                filter: "ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_placements_opened_by_employee_account_id",
                table: "placements",
                column: "opened_by_employee_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_placements_package_id",
                table: "placements",
                column: "package_id",
                unique: true,
                filter: "ended_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "placements");

            migrationBuilder.DropTable(
                name: "packages");
        }
    }
}
