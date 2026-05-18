using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProvisioningCodeStationAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "station_access",
                table: "provisioning_codes");

            migrationBuilder.CreateTable(
                name: "pickup_stations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_number = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pickup_stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "station_claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    pickup_station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    location_longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    location_address = table.Column<string>(type: "text", nullable: false),
                    location_house_number = table.Column<string>(type: "text", nullable: false),
                    location_zip_code = table.Column<string>(type: "text", nullable: false),
                    location_city = table.Column<string>(type: "text", nullable: false),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lockers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    pickup_station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locker_number = table.Column<int>(type: "integer", nullable: false),
                    is_serviceable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lockers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lockers_pickup_stations_pickup_station_id",
                        column: x => x.pickup_station_id,
                        principalTable: "pickup_stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lockers_pickup_station_id",
                table: "lockers",
                column: "pickup_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_pickup_stations_serial_number",
                table: "pickup_stations",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_station_claims_pickup_station_id",
                table: "station_claims",
                column: "pickup_station_id",
                unique: true,
                filter: "released_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lockers");

            migrationBuilder.DropTable(
                name: "station_claims");

            migrationBuilder.DropTable(
                name: "pickup_stations");

            migrationBuilder.AddColumn<string>(
                name: "station_access",
                table: "provisioning_codes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
