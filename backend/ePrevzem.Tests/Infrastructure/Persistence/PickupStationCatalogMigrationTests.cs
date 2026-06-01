using ePrevzem.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ePrevzem.Tests.Infrastructure.Persistence;

public class PickupStationCatalogMigrationTests
{
    [Fact]
    public void Up_inserts_ten_stations_and_one_hundred_serviceable_lockers()
    {
        var migration = new Lockers_SeedPickupStationCatalog();
        var inserts = migration.UpOperations.OfType<InsertDataOperation>().ToList();

        var stationInsert = inserts.Should().ContainSingle(x => x.Table == "pickup_stations").Which;
        stationInsert.Values.GetLength(0).Should().Be(10);
        Enumerable.Range(1, 10)
            .Select(number => $"EP-PM-{number:000}")
            .Should()
            .BeEquivalentTo(GetColumnValues<string>(stationInsert, "serial_number"));

        var lockerInsert = inserts.Should().ContainSingle(x => x.Table == "lockers").Which;
        lockerInsert.Values.GetLength(0).Should().Be(100);
        GetColumnValues<bool>(lockerInsert, "is_serviceable").Should().OnlyContain(value => value);
        GetColumnValues<int>(lockerInsert, "locker_number")
            .Should()
            .BeEquivalentTo(Enumerable.Repeat(Enumerable.Range(1, 10), 10).SelectMany(x => x));
    }

    private static IEnumerable<T> GetColumnValues<T>(InsertDataOperation insert, string columnName)
    {
        var columnIndex = Array.IndexOf(insert.Columns, columnName);
        columnIndex.Should().BeGreaterThanOrEqualTo(0);

        for (var row = 0; row < insert.Values.GetLength(0); row++)
            yield return (T)insert.Values[row, columnIndex]!;
    }
}
