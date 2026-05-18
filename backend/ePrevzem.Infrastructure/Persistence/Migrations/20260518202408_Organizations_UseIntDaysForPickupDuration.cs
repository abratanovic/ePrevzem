using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Organizations_UseIntDaysForPickupDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "default_pickup_duration",
                table: "organizations",
                newName: "default_pickup_duration_in_days");

            migrationBuilder.Sql(
                "ALTER TABLE organizations ALTER COLUMN default_pickup_duration_in_days TYPE integer " +
                "USING EXTRACT(EPOCH FROM default_pickup_duration_in_days)::integer / 86400");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "default_pickup_duration_in_days",
                table: "organizations",
                newName: "default_pickup_duration");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "default_pickup_duration",
                table: "organizations",
                type: "interval",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
