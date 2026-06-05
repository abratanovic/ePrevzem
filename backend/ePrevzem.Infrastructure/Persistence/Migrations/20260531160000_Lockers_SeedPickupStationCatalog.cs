using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ePrevzem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EPrevzemDbContext))]
[Migration("20260531160000_Lockers_SeedPickupStationCatalog")]
public sealed class Lockers_SeedPickupStationCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var stations = new object[10, 3];
        var lockers = new object[100, 4];

        for (var stationNumber = 1; stationNumber <= 10; stationNumber++)
        {
            var stationId = StationId(stationNumber);
            stations[stationNumber - 1, 0] = stationId;
            stations[stationNumber - 1, 1] = $"EP-PM-{stationNumber:000}";
            stations[stationNumber - 1, 2] = new DateTimeOffset(2026, 2, stationNumber + 9, 8, 0, 0, TimeSpan.Zero);

            for (var lockerNumber = 1; lockerNumber <= 10; lockerNumber++)
            {
                var lockerIndex = (stationNumber - 1) * 10 + lockerNumber - 1;
                lockers[lockerIndex, 0] = LockerId(stationNumber, lockerNumber);
                lockers[lockerIndex, 1] = stationId;
                lockers[lockerIndex, 2] = lockerNumber;
                lockers[lockerIndex, 3] = true;
            }
        }

        migrationBuilder.InsertData(
            table: "pickup_stations",
            columns: new[] { "Id", "serial_number", "created_at" },
            columnTypes: new[] { "uuid", "text", "timestamp with time zone" },
            values: stations);

        migrationBuilder.InsertData(
            table: "lockers",
            columns: new[] { "Id", "pickup_station_id", "locker_number", "is_serviceable" },
            columnTypes: new[] { "uuid", "uuid", "integer", "boolean" },
            values: lockers);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Pickup station catalog seed migration is forward-only.");

    private static Guid StationId(int stationNumber)
        => Guid.Parse($"10000000-0000-4000-8000-{stationNumber:000000000000}");

    private static Guid LockerId(int stationNumber, int lockerNumber)
        => Guid.Parse($"20000000-{stationNumber:0000}-4000-8000-{lockerNumber:000000000000}");
}
