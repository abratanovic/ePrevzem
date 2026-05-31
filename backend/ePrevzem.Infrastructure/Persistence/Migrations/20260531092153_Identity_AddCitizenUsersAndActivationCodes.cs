using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Identity_AddCitizenUsersAndActivationCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "citizen_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    emso = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    onboarded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citizen_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "citizen_activation_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    citizen_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citizen_activation_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_citizen_activation_codes_citizen_users_citizen_user_id",
                        column: x => x.citizen_user_id,
                        principalTable: "citizen_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "citizen_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    citizen_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    device_fingerprint = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    citizen_user_id1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citizen_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_citizen_devices_citizen_users_citizen_user_id1",
                        column: x => x.citizen_user_id1,
                        principalTable: "citizen_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_citizen_activation_codes_citizen_user_id",
                table: "citizen_activation_codes",
                column: "citizen_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_citizen_activation_codes_code",
                table: "citizen_activation_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_citizen_devices_citizen_user_id1",
                table: "citizen_devices",
                column: "citizen_user_id1");

            migrationBuilder.CreateIndex(
                name: "IX_citizen_users_emso",
                table: "citizen_users",
                column: "emso",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "citizen_activation_codes");

            migrationBuilder.DropTable(
                name: "citizen_devices");

            migrationBuilder.DropTable(
                name: "citizen_users");
        }
    }
}
