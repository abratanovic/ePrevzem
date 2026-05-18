using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.RegisterPickupStation;
using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Lockers;

public class RegisterPickupStationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    private static RegisterPickupStationCommandHandler BuildHandler(
        TestPickupStationRepository? repo = null,
        TestUnitOfWorkForStation? uow = null)
        => new(
            repo ?? new TestPickupStationRepository(),
            uow ?? new TestUnitOfWorkForStation(),
            new TestClockForStation(Now));

    private static RegisterPickupStationCommand ValidCommand(
        string serial = "SN-001",
        IReadOnlyList<int>? lockers = null)
        => new(serial, lockers ?? new[] { 1, 2, 3 });

    [Fact]
    public async Task Handle_with_valid_command_persists_station()
    {
        var repo = new TestPickupStationRepository();
        var handler = BuildHandler(repo: repo);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repo.Items.Should().HaveCount(1);
        repo.Items[0].SerialNumber.Should().Be("SN-001");
    }

    [Fact]
    public async Task Handle_with_valid_command_persists_correct_lockers()
    {
        var repo = new TestPickupStationRepository();
        var handler = BuildHandler(repo: repo);

        await handler.Handle(ValidCommand(lockers: new[] { 5, 10, 15 }), CancellationToken.None);

        repo.Items[0].Lockers.Select(l => l.LockerNumber).Should().BeEquivalentTo(new[] { 5, 10, 15 });
    }

    [Fact]
    public async Task Handle_with_valid_command_returns_correct_response()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.SerialNumber.Should().Be("SN-001");
        result.Lockers.Should().HaveCount(3);
        result.CreatedAt.Should().Be(Now);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_with_valid_command_calls_save_changes()
    {
        var uow = new TestUnitOfWorkForStation();
        var handler = BuildHandler(uow: uow);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_duplicate_serial_number_throws()
    {
        var repo = new TestPickupStationRepository();
        repo.AddExistingSerialNumber("SN-001");
        var handler = BuildHandler(repo: repo);

        var act = () => handler.Handle(ValidCommand(serial: "SN-001"), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSerialNumberException>();
    }

    [Fact]
    public async Task Handle_with_duplicate_serial_number_does_not_persist()
    {
        var repo = new TestPickupStationRepository();
        repo.AddExistingSerialNumber("SN-001");
        var handler = BuildHandler(repo: repo);

        try { await handler.Handle(ValidCommand(serial: "SN-001"), CancellationToken.None); }
        catch (DuplicateSerialNumberException) { }

        repo.Items.Should().BeEmpty();
    }
}

public sealed class TestPickupStationRepository : IPickupStationRepository
{
    public List<PickupStation> Items { get; } = new();
    private readonly HashSet<string> _existingSerialNumbers = new();

    public void AddExistingSerialNumber(string serial) => _existingSerialNumbers.Add(serial);

    public Task<PickupStation?> GetByIdAsync(PickupStationId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<PickupStation?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.SerialNumber == serialNumber));

    public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_existingSerialNumbers.Contains(serialNumber));

    public Task AddAsync(PickupStation station, CancellationToken cancellationToken = default)
    {
        Items.Add(station);
        return Task.CompletedTask;
    }
}

public sealed class TestClockForStation : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForStation(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForStation : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}
