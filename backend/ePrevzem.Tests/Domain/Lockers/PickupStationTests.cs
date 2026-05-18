using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class PickupStationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static Location ValidLocation() => Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");

    [Fact]
    public void Create_with_valid_fields_constructs_station()
    {
        var id = PickupStationId.New();
        var station = PickupStation.Create(id, ValidLocation(), Now);

        station.Id.Should().Be(id);
        station.Location.Should().Be(ValidLocation());
        station.CreatedAt.Should().Be(Now);
        station.Lockers.Should().BeEmpty();
    }

    [Fact]
    public void AddLocker_appends_to_lockers_collection()
    {
        var station = PickupStation.Create(PickupStationId.New(), ValidLocation(), Now);
        var locker = station.AddLocker(LockerId.New(), 1);

        station.Lockers.Should().ContainSingle().Which.Should().BeSameAs(locker);
    }

    [Fact]
    public void AddLocker_with_duplicate_number_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), ValidLocation(), Now);
        station.AddLocker(LockerId.New(), 1);

        var act = () => station.AddLocker(LockerId.New(), 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*locker number*1*");
    }

    [Fact]
    public void AddLocker_with_non_positive_number_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), ValidLocation(), Now);
        var act = () => station.AddLocker(LockerId.New(), 0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("lockerNumber");
    }
}
