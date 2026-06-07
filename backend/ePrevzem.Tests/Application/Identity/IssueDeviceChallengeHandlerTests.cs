using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class IssueDeviceChallengeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly CitizenUserId CitizenId = CitizenUserId.New();
    private static readonly CitizenDeviceId CitizenDeviceId = CitizenDeviceId.New();
    private static readonly EmployeeAccountId EmployeeId = EmployeeAccountId.New();
    private static readonly EmployeeDeviceId EmployeeDeviceId = EmployeeDeviceId.New();
    private static readonly OrganizationId OrgId = OrganizationId.New();

    private static IssueDeviceChallengeCommandHandler BuildHandler(
        TestCitizenUserRepoForChallenge? citizenUserRepo = null,
        TestEmployeeAccountRepoForChallenge? employeeRepo = null,
        TestDeviceChallengeRepo? challengeRepo = null,
        TestUnitOfWorkForChallenge? unitOfWork = null)
    {
        citizenUserRepo ??= DefaultCitizenUserRepo();
        employeeRepo ??= new TestEmployeeAccountRepoForChallenge();
        challengeRepo ??= new TestDeviceChallengeRepo();
        unitOfWork ??= new TestUnitOfWorkForChallenge();

        return new IssueDeviceChallengeCommandHandler(
            citizenUserRepo,
            employeeRepo,
            challengeRepo,
            unitOfWork,
            new TestClockForChallenge(Now));
    }

    private static TestCitizenUserRepoForChallenge DefaultCitizenUserRepo()
    {
        var repo = new TestCitizenUserRepoForChallenge();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var device = citizen.RegisterDevice(CitizenDeviceId, new byte[] { 1, 2, 3 }, "fingerprint", "MyPhone", Now);
        repo.Add(citizen);
        return repo;
    }

    private static TestEmployeeAccountRepoForChallenge DefaultEmployeeRepo()
    {
        var repo = new TestEmployeeAccountRepoForChallenge();
        var employee = EmployeeAccount.Create(
            EmployeeId,
            OrgId,
            "Ana", "Kovač", "ana@example.com",
            new[] { EmployeeAccountRole.Operator },
            new[] { PickupStationId.New() },
            ProvisioningCodeId.New(),
            Now);
        var device = employee.RegisterDevice(EmployeeDeviceId, new byte[] { 4, 5, 6 }, "fingerprint", "LaptopPC", Now);
        repo.Add(employee);
        return repo;
    }

    [Fact]
    public async Task Handle_with_valid_citizen_device_returns_challenge()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(
            new IssueDeviceChallengeCommand(CitizenDeviceId.Value),
            CancellationToken.None);

        result.Challenge.Should().NotBeEmpty();
        // Base64 of 32 bytes is 44 characters (32 * 4/3 = 42.67, rounded up with padding)
        result.Challenge.Length.Should().Be(44);
        result.ExpiresAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public async Task Handle_with_valid_employee_device_returns_challenge()
    {
        var employeeRepo = DefaultEmployeeRepo();
        var handler = BuildHandler(employeeRepo: employeeRepo);

        var result = await handler.Handle(
            new IssueDeviceChallengeCommand(EmployeeDeviceId.Value),
            CancellationToken.None);

        result.Challenge.Should().NotBeEmpty();
        result.Challenge.Length.Should().Be(44);
        result.ExpiresAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public async Task Handle_persists_challenge()
    {
        var challengeRepo = new TestDeviceChallengeRepo();
        var handler = BuildHandler(challengeRepo: challengeRepo);

        await handler.Handle(
            new IssueDeviceChallengeCommand(CitizenDeviceId.Value),
            CancellationToken.None);

        challengeRepo.Challenges.Should().HaveCount(1);
        var challenge = challengeRepo.Challenges.First();
        challenge.DeviceId.Should().Be(CitizenDeviceId.Value);
        challenge.DeviceKind.Should().Be(DeviceKind.Citizen);
        challenge.ExpiresAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public async Task Handle_calls_save_changes()
    {
        var unitOfWork = new TestUnitOfWorkForChallenge();
        var handler = BuildHandler(unitOfWork: unitOfWork);

        await handler.Handle(
            new IssueDeviceChallengeCommand(CitizenDeviceId.Value),
            CancellationToken.None);

        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_unknown_device_throws()
    {
        var citizenRepo = new TestCitizenUserRepoForChallenge();
        var employeeRepo = new TestEmployeeAccountRepoForChallenge();
        var handler = BuildHandler(citizenUserRepo: citizenRepo, employeeRepo: employeeRepo);

        var act = () => handler.Handle(
            new IssueDeviceChallengeCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<DeviceNotFoundException>();
    }

    [Fact]
    public async Task Handle_with_revoked_citizen_device_throws()
    {
        var citizenRepo = new TestCitizenUserRepoForChallenge();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var device = citizen.RegisterDevice(CitizenDeviceId, new byte[] { 1, 2, 3 }, "fingerprint", "MyPhone", Now);
        citizen.RevokeDevice(CitizenDeviceId, Now.AddMinutes(1));
        citizenRepo.Add(citizen);

        var handler = BuildHandler(citizenUserRepo: citizenRepo);
        var act = () => handler.Handle(
            new IssueDeviceChallengeCommand(CitizenDeviceId.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<DeviceNotFoundException>();
    }

    [Fact]
    public async Task Handle_with_revoked_employee_device_throws()
    {
        var employeeRepo = new TestEmployeeAccountRepoForChallenge();
        var employee = EmployeeAccount.Create(
            EmployeeId,
            OrgId,
            "Ana", "Kovač", "ana@example.com",
            new[] { EmployeeAccountRole.Operator },
            new[] { PickupStationId.New() },
            ProvisioningCodeId.New(),
            Now);
        var device = employee.RegisterDevice(EmployeeDeviceId, new byte[] { 4, 5, 6 }, "fingerprint", "LaptopPC", Now);
        employee.RevokeDevice(EmployeeDeviceId, Now.AddMinutes(1));
        employeeRepo.Add(employee);

        var handler = BuildHandler(employeeRepo: employeeRepo);
        var act = () => handler.Handle(
            new IssueDeviceChallengeCommand(EmployeeDeviceId.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<DeviceNotFoundException>();
    }
}

public sealed class TestCitizenUserRepoForChallenge : ICitizenUserRepository
{
    private readonly List<CitizenUser> _items = new();

    public void Add(CitizenUser user) => _items.Add(user);

    public Task<CitizenUser?> GetByIdAsync(CitizenUserId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<CitizenUser?> GetByEmsoAsync(string emso, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Emso == emso));

    public Task<CitizenUser?> GetByCitizenDeviceIdAsync(CitizenDeviceId deviceId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Devices.Any(d => d.Id == deviceId)));

    public Task AddAsync(CitizenUser user, CancellationToken cancellationToken = default)
    {
        _items.Add(user);
        return Task.CompletedTask;
    }
}

public sealed class TestEmployeeAccountRepoForChallenge : IEmployeeAccountRepository
{
    private readonly List<EmployeeAccount> _items = new();

    public void Add(EmployeeAccount account) => _items.Add(account);

    public Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == normalizedEmail));

    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Devices.Any(d => d.Id == deviceId)));

    public Task AddAsync(EmployeeAccount account, CancellationToken cancellationToken = default)
    {
        _items.Add(account);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EmployeeAccount>>(_items);
}

public sealed class TestDeviceChallengeRepo : IDeviceChallengeRepository
{
    private readonly List<DeviceChallenge> _items = new();

    public List<DeviceChallenge> Challenges => _items;

    public Task AddAsync(DeviceChallenge challenge, CancellationToken cancellationToken = default)
    {
        _items.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<DeviceChallenge?> GetLatestActiveAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken = default)
        => Task.FromResult(_items
            .Where(x => x.DeviceId == deviceId && x.IsConsumable(now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault());
}

public sealed class TestUnitOfWorkForChallenge : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}

public sealed class TestClockForChallenge : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForChallenge(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}
