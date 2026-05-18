using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class LockerTests
{
    [Fact]
    public void Locker_added_via_station_is_serviceable_by_default()
    {
        var station = NewStation();
        var locker = station.AddLocker(LockerId.New(), lockerNumber: 1);

        locker.LockerNumber.Should().Be(1);
        locker.IsServiceable.Should().BeTrue();
        locker.PickupStationId.Should().Be(station.Id);
    }

    [Fact]
    public void MarkOutOfService_flips_flag_to_false()
    {
        var station = NewStation();
        var locker = station.AddLocker(LockerId.New(), 1);

        locker.MarkOutOfService();

        locker.IsServiceable.Should().BeFalse();
    }

    [Fact]
    public void MarkServiceable_flips_flag_to_true()
    {
        var station = NewStation();
        var locker = station.AddLocker(LockerId.New(), 1);
        locker.MarkOutOfService();

        locker.MarkServiceable();

        locker.IsServiceable.Should().BeTrue();
    }

    private static PickupStation NewStation() => PickupStation.Create(
        PickupStationId.New(),
        Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana"),
        new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
}
