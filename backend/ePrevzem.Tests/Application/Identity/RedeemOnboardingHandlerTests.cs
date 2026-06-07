using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.RedeemOnboarding;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class RedeemOnboardingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly CitizenUserId CitizenId = CitizenUserId.New();
    private static readonly OrganizationId OrgId = OrganizationId.New();

    private static RedeemOnboardingCodeCommandHandler BuildHandler(
        TestCitizenActivationCodeRepoForRedeem? citizenCodeRepo = null,
        TestProvisioningCodeRepoForRedeem? provisioningCodeRepo = null,
        TestCitizenUserRepoForRedeem? citizenUserRepo = null,
        TestEmployeeAccountRepoForRedeem? employeeRepo = null,
        TestOrganizationRepoForRedeem? orgRepo = null,
        TestRefreshTokenRepositoryForRedeem? refreshTokenRepo = null,
        TestUnitOfWorkForRedeem? unitOfWork = null)
    {
        citizenCodeRepo ??= new TestCitizenActivationCodeRepoForRedeem();
        provisioningCodeRepo ??= new TestProvisioningCodeRepoForRedeem();
        citizenUserRepo ??= DefaultCitizenUserRepo();
        employeeRepo ??= new TestEmployeeAccountRepoForRedeem();
        orgRepo ??= DefaultOrgRepo();
        refreshTokenRepo ??= new TestRefreshTokenRepositoryForRedeem();
        unitOfWork ??= new TestUnitOfWorkForRedeem();

        return new RedeemOnboardingCodeCommandHandler(
            citizenCodeRepo,
            provisioningCodeRepo,
            citizenUserRepo,
            employeeRepo,
            orgRepo,
            refreshTokenRepo,
            unitOfWork,
            new TestClockForRedeem(Now),
            new TestTokenServiceForRedeem());
    }

    private static TestCitizenUserRepoForRedeem DefaultCitizenUserRepo()
    {
        var repo = new TestCitizenUserRepoForRedeem();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        repo.Add(citizen);
        return repo;
    }

    private static TestOrganizationRepoForRedeem DefaultOrgRepo()
    {
        var repo = new TestOrganizationRepoForRedeem();
        repo.Add(Organization.Create(OrgId, "Test Org", "SI00000001", "0000001000",
            TimeSpan.FromDays(7), Now));
        return repo;
    }

    [Fact]
    public async Task Handle_with_valid_citizen_code_registers_device_and_issues_tokens()
    {
        const string code = "CITIZEN123";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";
        const string fingerprint = "device-fingerprint-123";

        var citizenCodeRepo = new TestCitizenActivationCodeRepoForRedeem();
        var citizenCode = CitizenActivationCode.Issue(
            CitizenActivationCodeId.New(),
            CitizenId,
            code,
            Now,
            Now.AddHours(24));
        citizenCodeRepo.Add(citizenCode);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRedeem();
        var handler = BuildHandler(
            citizenCodeRepo: citizenCodeRepo,
            refreshTokenRepo: refreshTokenRepo);

        var result = await handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, fingerprint, "My Phone"),
            CancellationToken.None);

        result.Role.Should().Be("Citizen");
        result.FirstName.Should().Be("Janez");
        result.LastName.Should().Be("Novak");
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        result.AccessTokenExpiresAt.Should().Be(Now.AddHours(1));
        result.RefreshTokenExpiresAt.Should().Be(Now.AddDays(14));
        refreshTokenRepo.Items.Should().HaveCount(1);
        refreshTokenRepo.Items[0].CitizenUserId.Should().Be(CitizenId);
    }

    [Fact]
    public async Task Handle_with_expired_citizen_code_throws_expired()
    {
        const string code = "EXPIRED_CITIZEN";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";

        var citizenCodeRepo = new TestCitizenActivationCodeRepoForRedeem();
        var citizenCode = CitizenActivationCode.Issue(
            CitizenActivationCodeId.New(),
            CitizenId,
            code,
            Now.AddDays(-2),
            Now.AddDays(-1));
        citizenCodeRepo.Add(citizenCode);

        var handler = BuildHandler(citizenCodeRepo: citizenCodeRepo);

        var act = () => handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, "fingerprint", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_already_redeemed_citizen_code_throws_expired()
    {
        const string code = "REDEEMED_CITIZEN";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";

        var citizenCodeRepo = new TestCitizenActivationCodeRepoForRedeem();
        var citizenCode = CitizenActivationCode.Issue(
            CitizenActivationCodeId.New(),
            CitizenId,
            code,
            Now,
            Now.AddHours(24));
        citizenCode.Redeem(Now.AddMinutes(5));
        citizenCodeRepo.Add(citizenCode);

        var handler = BuildHandler(citizenCodeRepo: citizenCodeRepo);

        var act = () => handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, "fingerprint", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_valid_employee_code_for_new_account_creates_and_registers_device()
    {
        const string code = "EMPLOYEE123";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";
        const string fingerprint = "laptop-fingerprint";

        var provisioningCodeRepo = new TestProvisioningCodeRepoForRedeem();
        var provisioningCode = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrgId,
            code,
            PersonalInfo.Create("Ana", "Kovač", "ana@example.com"),
            new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager },
            OrganizationAdminAccountId.New(),
            Now,
            Now.AddHours(72),
            isReprovisioningOf: null);
        provisioningCodeRepo.Add(provisioningCode);

        var employeeRepo = new TestEmployeeAccountRepoForRedeem();
        var refreshTokenRepo = new TestRefreshTokenRepositoryForRedeem();
        var handler = BuildHandler(
            provisioningCodeRepo: provisioningCodeRepo,
            employeeRepo: employeeRepo,
            refreshTokenRepo: refreshTokenRepo);

        var result = await handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, fingerprint, "Laptop"),
            CancellationToken.None);

        result.Role.Should().Be("Employee");
        result.FirstName.Should().Be("Ana");
        result.LastName.Should().Be("Kovač");
        result.OrganizationId.Should().Be(OrgId.Value);
        result.OrganizationName.Should().Be("Test Org");
        result.Roles.Should().Contain("Operator").And.Contain("RecordManager");
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        employeeRepo.Items.Should().HaveCount(1);
        employeeRepo.Items[0].FirstName.Should().Be("Ana");
        refreshTokenRepo.Items.Should().HaveCount(1);
        refreshTokenRepo.Items[0].EmployeeAccountId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_with_employee_code_for_reprovi_account_resolves_existing_account()
    {
        const string code = "REPROV_EMPLOYEE";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";

        var employeeId = EmployeeAccountId.New();
        var existingEmployee = EmployeeAccount.Create(
            employeeId,
            OrgId,
            "Ana", "Kovač", "ana@example.com",
            new[] { EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(),
            Now);

        var provisioningCodeRepo = new TestProvisioningCodeRepoForRedeem();
        var provisioningCode = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrgId,
            code,
            PersonalInfo.Create("Ana", "Kovač", "ana@example.com"),
            new[] { EmployeeAccountRole.RecordManager },
            OrganizationAdminAccountId.New(),
            Now,
            Now.AddHours(72),
            isReprovisioningOf: employeeId);
        provisioningCodeRepo.Add(provisioningCode);

        var employeeRepo = new TestEmployeeAccountRepoForRedeem();
        employeeRepo.Add(existingEmployee);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRedeem();
        var handler = BuildHandler(
            provisioningCodeRepo: provisioningCodeRepo,
            employeeRepo: employeeRepo,
            refreshTokenRepo: refreshTokenRepo);

        var result = await handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, "fingerprint", null),
            CancellationToken.None);

        result.Role.Should().Be("Employee");
        result.FirstName.Should().Be("Ana");
        employeeRepo.Items.Should().HaveCount(1); // Not added again
        refreshTokenRepo.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_with_unknown_code_throws_not_found()
    {
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";

        var handler = BuildHandler();

        var act = () => handler.Handle(
            new RedeemOnboardingCodeCommand("UNKNOWN", publicKeyPem, "fingerprint", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_with_expired_employee_code_throws_expired()
    {
        const string code = "EXPIRED_EMPLOYEE";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";

        var provisioningCodeRepo = new TestProvisioningCodeRepoForRedeem();
        var provisioningCode = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrgId,
            code,
            PersonalInfo.Create("Ana", "Kovač", "ana@example.com"),
            new[] { EmployeeAccountRole.Operator },
            OrganizationAdminAccountId.New(),
            Now.AddDays(-3),
            Now.AddDays(-2),
            isReprovisioningOf: null);
        provisioningCodeRepo.Add(provisioningCode);

        var handler = BuildHandler(provisioningCodeRepo: provisioningCodeRepo);

        var act = () => handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, "fingerprint", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_existing_account_from_add_member_flow_does_not_duplicate()
    {
        const string code = "ADDMEMBER123";
        const string publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\n-----END PUBLIC KEY-----";
        const string fingerprint = "device-fingerprint";

        var provisioningCodeId = ProvisioningCodeId.New();
        var existingEmployee = EmployeeAccount.Create(
            EmployeeAccountId.New(),
            OrgId,
            "Marko", "Marković", "marko@example.com",
            new[] { EmployeeAccountRole.RecordManager, EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            provisioningCodeId,
            Now);

        var provisioningCodeRepo = new TestProvisioningCodeRepoForRedeem();
        var provisioningCode = ProvisioningCode.Issue(
            provisioningCodeId,
            OrgId,
            code,
            PersonalInfo.Create("Marko", "Marković", "marko@example.com"),
            new[] { EmployeeAccountRole.RecordManager, EmployeeAccountRole.Operator },
            OrganizationAdminAccountId.New(),
            Now,
            Now.AddHours(72),
            isReprovisioningOf: null);
        provisioningCodeRepo.Add(provisioningCode);

        var employeeRepo = new TestEmployeeAccountRepoForRedeem();
        employeeRepo.Add(existingEmployee);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForRedeem();
        var handler = BuildHandler(
            provisioningCodeRepo: provisioningCodeRepo,
            employeeRepo: employeeRepo,
            refreshTokenRepo: refreshTokenRepo);

        var result = await handler.Handle(
            new RedeemOnboardingCodeCommand(code, publicKeyPem, fingerprint, "Mobile"),
            CancellationToken.None);

        result.Role.Should().Be("Employee");
        result.FirstName.Should().Be("Marko");
        result.LastName.Should().Be("Marković");
        employeeRepo.Items.Should().HaveCount(1);
        employeeRepo.Items[0].Id.Should().Be(existingEmployee.Id);
        refreshTokenRepo.Items.Should().HaveCount(1);
        refreshTokenRepo.Items[0].EmployeeAccountId.Should().NotBeNull();
    }
}

public sealed class TestCitizenActivationCodeRepoForRedeem : ICitizenActivationCodeRepository
{
    private readonly List<CitizenActivationCode> _items = new();

    public void Add(CitizenActivationCode code) => _items.Add(code);

    public Task<CitizenActivationCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Code == code));

    public Task AddAsync(CitizenActivationCode code, CancellationToken cancellationToken = default)
    {
        _items.Add(code);
        return Task.CompletedTask;
    }
}

public sealed class TestProvisioningCodeRepoForRedeem : IProvisioningCodeRepository
{
    private readonly List<ProvisioningCode> _items = new();

    public void Add(ProvisioningCode code) => _items.Add(code);

    public Task<ProvisioningCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Code == code));

    public Task AddAsync(ProvisioningCode provisioningCode, CancellationToken cancellationToken = default)
    {
        _items.Add(provisioningCode);
        return Task.CompletedTask;
    }
}

public sealed class TestCitizenUserRepoForRedeem : ICitizenUserRepository
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

public sealed class TestEmployeeAccountRepoForRedeem : IEmployeeAccountRepository
{
    private readonly List<EmployeeAccount> _items = new();

    public List<EmployeeAccount> Items => _items;

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

public sealed class TestOrganizationRepoForRedeem : IOrganizationRepository
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

public sealed class TestRefreshTokenRepositoryForRedeem : IRefreshTokenRepository
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

public sealed class TestUnitOfWorkForRedeem : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}

public sealed class TestClockForRedeem : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForRedeem(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestTokenServiceForRedeem : ITokenService
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
