using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Lockers.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class PickupStationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private const string Serial = "SN-001";

    [Fact]
    public void Create_with_valid_fields_constructs_station()
    {
        var id = PickupStationId.New();
        var station = PickupStation.Create(id, Serial, Now);

        station.Id.Should().Be(id);
        station.SerialNumber.Should().Be(Serial);
        station.CreatedAt.Should().Be(Now);
        station.Lockers.Should().BeEmpty();
        station.DomainEvents.OfType<PickupStationCreated>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_serial_number_throws(string serial)
    {
        var act = () => PickupStation.Create(PickupStationId.New(), serial, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("serialNumber");
    }

    [Fact]
    public void AddLocker_appends_to_lockers_collection()
    {
        var station = PickupStation.Create(PickupStationId.New(), Serial, Now);
        station.ClearDomainEvents();

        var locker = station.AddLocker(LockerId.New(), 1, Now);

        station.Lockers.Should().ContainSingle().Which.Should().BeSameAs(locker);
        station.DomainEvents.OfType<LockerCreated>().Should().ContainSingle()
            .Which.LockerId.Should().Be(locker.Id);
    }

    [Fact]
    public void AddLocker_with_duplicate_number_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), Serial, Now);
        station.AddLocker(LockerId.New(), 1);

        var act = () => station.AddLocker(LockerId.New(), 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*locker number*1*");
    }

    [Fact]
    public void AddLocker_with_non_positive_number_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), Serial, Now);
        var act = () => station.AddLocker(LockerId.New(), 0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("lockerNumber");
    }

    [Fact]
    public void SetLockerServiceability_marks_requested_locker_out_of_service()
    {
        var station = PickupStation.Create(PickupStationId.New(), Serial, Now);
        var locker = station.AddLocker(LockerId.New(), 1);
        station.ClearDomainEvents();

        station.SetLockerServiceability(locker.Id, false, Now);

        locker.IsServiceable.Should().BeFalse();
        station.DomainEvents.OfType<LockerServiceabilityChanged>().Should().ContainSingle()
            .Which.IsServiceable.Should().BeFalse();
    }

    [Fact]
    public void SetLockerServiceability_with_locker_from_another_station_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), Serial, Now);

        var act = () => station.SetLockerServiceability(LockerId.New(), false);

        act.Should().Throw<InvalidOperationException>().WithMessage("*does not belong*");
    }
}
