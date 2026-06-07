using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.PeekOnboarding;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class PeekOnboardingCodeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly CitizenUserId CitizenId = CitizenUserId.New();
    private static readonly OrganizationId OrgId = OrganizationId.New();
    private const string CitizenCode = "CITIZEN123";
    private const string EmployeeCode = "EMPLOYEE456";

    private static PeekOnboardingCodeQueryHandler BuildHandler(
        TestCitizenActivationCodeRepo? citizenCodeRepo = null,
        TestCitizenUserRepoForPeekOnboarding? citizenUserRepo = null,
        TestProvisioningCodeRepoForPeekOnboarding? provisioningCodeRepo = null,
        TestOrgRepoForPeekOnboarding? orgRepo = null)
    {
        citizenCodeRepo ??= new TestCitizenActivationCodeRepo();
        citizenUserRepo ??= DefaultCitizenUserRepo();
        provisioningCodeRepo ??= new TestProvisioningCodeRepoForPeekOnboarding();
        orgRepo ??= DefaultOrgRepo();

        return new PeekOnboardingCodeQueryHandler(
            citizenCodeRepo,
            citizenUserRepo,
            provisioningCodeRepo,
            orgRepo,
            new TestClockForPeekOnboarding(Now));
    }

    private static TestCitizenUserRepoForPeekOnboarding DefaultCitizenUserRepo()
    {
        var repo = new TestCitizenUserRepoForPeekOnboarding();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        repo.Add(citizen);
        return repo;
    }

    private static TestOrgRepoForPeekOnboarding DefaultOrgRepo()
    {
        var repo = new TestOrgRepoForPeekOnboarding();
        repo.Add(Organization.Create(OrgId, "Test Org", "SI00000001", "0000001000",
            TimeSpan.FromDays(7), Now));
        return repo;
    }

    private static CitizenActivationCode IssueCitizenCode(bool expired = false)
    {
        var expiresAt = expired ? Now.AddDays(-1) : Now.AddHours(24);
        var issuedAt = expired ? Now.AddDays(-20) : Now;
        return CitizenActivationCode.Issue(
            CitizenActivationCodeId.New(),
            CitizenId,
            CitizenCode,
            issuedAt,
            expiresAt);
    }

    private static ProvisioningCode IssueEmployeeCode(bool expired = false, bool redeemed = false)
    {
        var expiresAt = expired ? Now.AddDays(-1) : Now.AddHours(24);
        var issuedAt = expired ? Now.AddDays(-20) : Now;

        var code = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrgId,
            EmployeeCode,
            PersonalInfo.Create("Ana", "Kovač", "ana@example.com"),
            new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager },
            OrganizationAdminAccountId.New(),
            issuedAt,
            expiresAt,
            null);

        if (redeemed)
            code.Redeem(issuedAt.AddMinutes(5), EmployeeAccountId.New());

        return code;
    }

    [Fact]
    public async Task Handle_with_valid_citizen_code_returns_citizen_preview()
    {
        var citizenCodeRepo = new TestCitizenActivationCodeRepo();
        citizenCodeRepo.Add(IssueCitizenCode());

        var handler = BuildHandler(citizenCodeRepo: citizenCodeRepo);
        var result = await handler.Handle(
            new PeekOnboardingCodeQuery(CitizenCode),
            CancellationToken.None);

        result.Kind.Should().Be("Citizen");
        result.FirstName.Should().Be("Janez");
        result.LastName.Should().Be("Novak");
        result.Email.Should().Be("janez@example.com");
        result.PhoneNumber.Should().Be("0123456789");
        result.OrganizationName.Should().BeNull();
        result.Roles.Should().BeEmpty();
        result.ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Handle_with_valid_employee_code_returns_employee_preview()
    {
        var provisioningCodeRepo = new TestProvisioningCodeRepoForPeekOnboarding();
        provisioningCodeRepo.Add(IssueEmployeeCode());

        var handler = BuildHandler(provisioningCodeRepo: provisioningCodeRepo);
        var result = await handler.Handle(
            new PeekOnboardingCodeQuery(EmployeeCode),
            CancellationToken.None);

        result.Kind.Should().Be("Employee");
        result.FirstName.Should().Be("Ana");
        result.LastName.Should().Be("Kovač");
        result.Email.Should().Be("ana@example.com");
        result.PhoneNumber.Should().BeNull();
        result.OrganizationName.Should().Be("Test Org");
        result.Roles.Should().HaveCount(2);
        result.Roles.Should().Contain("Operator");
        result.Roles.Should().Contain("RecordManager");
        result.ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Handle_with_unknown_code_throws_not_found()
    {
        var handler = BuildHandler();
        var act = () => handler.Handle(
            new PeekOnboardingCodeQuery("UNKNOWN"),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_with_expired_citizen_code_throws_expired()
    {
        var citizenCodeRepo = new TestCitizenActivationCodeRepo();
        citizenCodeRepo.Add(IssueCitizenCode(expired: true));

        var handler = BuildHandler(citizenCodeRepo: citizenCodeRepo);
        var act = () => handler.Handle(
            new PeekOnboardingCodeQuery(CitizenCode),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_expired_employee_code_throws_expired()
    {
        var provisioningCodeRepo = new TestProvisioningCodeRepoForPeekOnboarding();
        provisioningCodeRepo.Add(IssueEmployeeCode(expired: true));

        var handler = BuildHandler(provisioningCodeRepo: provisioningCodeRepo);
        var act = () => handler.Handle(
            new PeekOnboardingCodeQuery(EmployeeCode),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_redeemed_citizen_code_throws_expired()
    {
        var citizenCode = IssueCitizenCode();
        citizenCode.Redeem(Now.AddMinutes(5));

        var citizenCodeRepo = new TestCitizenActivationCodeRepo();
        citizenCodeRepo.Add(citizenCode);

        var handler = BuildHandler(citizenCodeRepo: citizenCodeRepo);
        var act = () => handler.Handle(
            new PeekOnboardingCodeQuery(CitizenCode),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_redeemed_employee_code_throws_expired()
    {
        var provisioningCodeRepo = new TestProvisioningCodeRepoForPeekOnboarding();
        provisioningCodeRepo.Add(IssueEmployeeCode(redeemed: true));

        var handler = BuildHandler(provisioningCodeRepo: provisioningCodeRepo);
        var act = () => handler.Handle(
            new PeekOnboardingCodeQuery(EmployeeCode),
            CancellationToken.None);

        await act.Should().ThrowAsync<OnboardingCodeExpiredException>();
    }
}

public sealed class TestCitizenActivationCodeRepo : ICitizenActivationCodeRepository
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

public sealed class TestCitizenUserRepoForPeekOnboarding : ICitizenUserRepository
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

public sealed class TestProvisioningCodeRepoForPeekOnboarding : IProvisioningCodeRepository
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

public sealed class TestOrgRepoForPeekOnboarding : IOrganizationRepository
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

public sealed class TestClockForPeekOnboarding : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForPeekOnboarding(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}
