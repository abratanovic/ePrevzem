using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceRefresh;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class RefreshDeviceTokenHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly CitizenUserId CitizenId = CitizenUserId.New();
    private static readonly EmployeeAccountId EmployeeId = EmployeeAccountId.New();
    private static readonly OrganizationId OrgId = OrganizationId.New();

    private static RefreshDeviceTokenCommandHandler BuildHandler(
        TestRefreshTokenRepositoryForRefresh? refreshTokenRepo = null,
        TestCitizenUserRepoForRefresh? citizenUserRepo = null,
        TestEmployeeAccountRepoForRefresh? employeeRepo = null,
        TestOrganizationRepoForRefresh? orgRepo = null,
        TestUnitOfWorkForRefresh? unitOfWork = null)
    {
        refreshTokenRepo ??= new TestRefreshTokenRepositoryForRefresh();
        citizenUserRepo ??= DefaultCitizenUserRepo();
        employeeRepo ??= new TestEmployeeAccountRepoForRefresh();
        orgRepo ??= DefaultOrgRepo();
        unitOfWork ??= new TestUnitOfWorkForRefresh();

        return new RefreshDeviceTokenCommandHandler(
            refreshTokenRepo,
            citizenUserRepo,
            employeeRepo,
            orgRepo,
            unitOfWork,
            new TestClockForRefresh(Now),
            new TestTokenServiceForRefresh());
    }

    private static TestCitizenUserRepoForRefresh DefaultCitizenUserRepo()
    {
        var repo = new TestCitizenUserRepoForRefresh();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        repo.Add(citizen);
        return repo;
    }

    private static TestOrganizationRepoForRefresh DefaultOrgRepo()
    {
        var repo = new TestOrganizationRepoForRefresh();
        repo.Add(Organization.Create(OrgId, "Test Org", "SI00000001", "0000001000",
            TimeSpan.FromDays(7), Now));
        return repo;
    }

    [Fact]
    public async Task Handle_with_valid_citizen_refresh_token_rotates_and_returns_new_pair()
    {
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var citizenRepo = new TestCitizenUserRepoForRefresh();
        citizenRepo.Add(citizen);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRefresh();
        var existingToken = RefreshToken.IssueForCitizen(
            RefreshTokenId.New(),
            citizen.Id,
            ComputeTokenHash("refresh_token_citizen"),
            Now.AddDays(14),
            Now);
        refreshTokenRepo.Items.Add(existingToken);

        var unitOfWork = new TestUnitOfWorkForRefresh();
        var handler = BuildHandler(
            refreshTokenRepo: refreshTokenRepo,
            citizenUserRepo: citizenRepo,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new RefreshDeviceTokenCommand("refresh_token_citizen"),
            CancellationToken.None);

        result.Role.Should().Be("Citizen");
        result.FirstName.Should().Be("Janez");
        result.LastName.Should().Be("Novak");
        result.DeviceId.Should().Be(Guid.Empty); // Unknown in refresh flow
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        refreshTokenRepo.Items.Should().HaveCount(2);
        existingToken.RevokedAt.Should().Be(Now);
        existingToken.ReplacedByTokenId.Should().NotBeNull();
        existingToken.DomainEvents.OfType<RefreshTokenRotated>().Should().ContainSingle();
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_valid_employee_refresh_token_rotates_and_returns_new_pair()
    {
        var employee = EmployeeAccount.Create(
            EmployeeId,
            OrgId,
            "Ana", "Kovač", "ana@example.com",
            new[] { EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(),
            Now);
        var employeeRepo = new TestEmployeeAccountRepoForRefresh();
        employeeRepo.Add(employee);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRefresh();
        var existingToken = RefreshToken.IssueForEmployee(
            RefreshTokenId.New(),
            employee.Id,
            ComputeTokenHash("refresh_token_employee"),
            Now.AddDays(14),
            Now);
        refreshTokenRepo.Items.Add(existingToken);

        var unitOfWork = new TestUnitOfWorkForRefresh();
        var orgRepo = DefaultOrgRepo();
        var handler = BuildHandler(
            refreshTokenRepo: refreshTokenRepo,
            employeeRepo: employeeRepo,
            orgRepo: orgRepo,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new RefreshDeviceTokenCommand("refresh_token_employee"),
            CancellationToken.None);

        result.Role.Should().Be("Employee");
        result.FirstName.Should().Be("Ana");
        result.LastName.Should().Be("Kovač");
        result.OrganizationId.Should().Be(OrgId.Value);
        result.OrganizationName.Should().Be("Test Org");
        result.Roles.Should().Contain("Operator");
        result.DeviceId.Should().Be(Guid.Empty);
        refreshTokenRepo.Items.Should().HaveCount(2);
        existingToken.RevokedAt.Should().Be(Now);
        existingToken.ReplacedByTokenId.Should().NotBeNull();
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_unknown_refresh_token_throws_invalid_refresh_token()
    {
        var handler = BuildHandler();

        var act = () => handler.Handle(
            new RefreshDeviceTokenCommand("unknown_token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_expired_refresh_token_throws_invalid_refresh_token()
    {
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var citizenRepo = new TestCitizenUserRepoForRefresh();
        citizenRepo.Add(citizen);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRefresh();
        var expiredToken = RefreshToken.IssueForCitizen(
            RefreshTokenId.New(),
            citizen.Id,
            ComputeTokenHash("expired_token"),
            Now.AddMinutes(-1),
            Now.AddDays(-1));
        refreshTokenRepo.Items.Add(expiredToken);

        var handler = BuildHandler(
            refreshTokenRepo: refreshTokenRepo,
            citizenUserRepo: citizenRepo);

        var act = () => handler.Handle(
            new RefreshDeviceTokenCommand("expired_token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_revoked_refresh_token_revokes_descendants_and_throws()
    {
        var nowAtHandler = new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", nowAtHandler);
        var citizenRepo = new TestCitizenUserRepoForRefresh();
        citizenRepo.Add(citizen);

        // Create both tokens at or before the handler's clock time
        var rootToken = RefreshToken.IssueForCitizen(
            RefreshTokenId.New(),
            citizen.Id,
            ComputeTokenHash("root_token"),
            nowAtHandler.AddDays(14),
            nowAtHandler.AddMinutes(-2));
        var descendantToken = RefreshToken.IssueForCitizen(
            RefreshTokenId.New(),
            citizen.Id,
            ComputeTokenHash("descendant_token"),
            nowAtHandler.AddDays(14),
            nowAtHandler.AddMinutes(-1));

        rootToken.Rotate(descendantToken.Id, nowAtHandler);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRefresh();
        refreshTokenRepo.Items.Add(rootToken);
        refreshTokenRepo.Items.Add(descendantToken);

        var unitOfWork = new TestUnitOfWorkForRefresh();
        // Create handler with clock at nowAtHandler
        refreshTokenRepo ??= new TestRefreshTokenRepositoryForRefresh();
        citizenRepo ??= DefaultCitizenUserRepo();
        var employeeRepo = new TestEmployeeAccountRepoForRefresh();
        var orgRepo = DefaultOrgRepo();
        unitOfWork ??= new TestUnitOfWorkForRefresh();

        var handler = new RefreshDeviceTokenCommandHandler(
            refreshTokenRepo,
            citizenRepo,
            employeeRepo,
            orgRepo,
            unitOfWork,
            new TestClockForRefresh(nowAtHandler),
            new TestTokenServiceForRefresh());

        var act = () => handler.Handle(
            new RefreshDeviceTokenCommand("root_token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
        rootToken.DomainEvents.OfType<RefreshTokenChainRevoked>().Should().ContainSingle();
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    private static string ComputeTokenHash(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

public sealed class TestRefreshTokenRepositoryForRefresh : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = new();

    public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.TokenHash == tokenHash));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Items.Add(refreshToken);
        return Task.CompletedTask;
    }
}

public sealed class TestCitizenUserRepoForRefresh : ICitizenUserRepository
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

public sealed class TestEmployeeAccountRepoForRefresh : IEmployeeAccountRepository
{
    private readonly List<EmployeeAccount> _items = new();

    public void Add(EmployeeAccount account) => _items.Add(account);

    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == normalizedEmail));

    public Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Devices.Any(d => d.Id == deviceId)));

    public Task<EmployeeAccount?> GetByProvisioningCodeIdAsync(ProvisioningCodeId provisioningCodeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.CreatedFromProvisioningCodeId == provisioningCodeId));

    public Task AddAsync(EmployeeAccount account, CancellationToken cancellationToken = default)
    {
        _items.Add(account);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EmployeeAccount>>(_items);
}

public sealed class TestOrganizationRepoForRefresh : IOrganizationRepository
{
    private readonly List<Organization> _orgs = new();

    public void Add(Organization org) => _orgs.Add(org);

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_orgs.SingleOrDefault(x => x.Id == id));

    public Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_orgs.Any(x => x.TaxNumber == taxNumber));

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_orgs.Any(x => x.RegistrationNumber == registrationNumber));

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _orgs.Add(organization);
        return Task.CompletedTask;
    }
}

public sealed class TestUnitOfWorkForRefresh : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}

public sealed class TestClockForRefresh : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForRefresh(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestTokenServiceForRefresh : ITokenService
{
    private int _accessTokenCounter = 0;
    private int _refreshTokenCounter = 0;

    public AccessTokenResult IssueAccessToken(SystemAdmin admin)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public AccessTokenResult IssueAccessToken(EmployeeAccount employee)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public AccessTokenResult IssueAccessToken(CitizenUser citizen)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now)
        => new($"refresh_plaintext_{++_refreshTokenCounter}", $"refresh_hash_{_refreshTokenCounter}", now.AddDays(14));

    private static class _clock
    {
        public static readonly DateTimeOffset UtcNow = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    }
}
