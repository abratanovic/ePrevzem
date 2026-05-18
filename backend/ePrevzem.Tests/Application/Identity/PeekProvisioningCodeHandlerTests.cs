using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.PeekProvisioningCode;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class PeekProvisioningCodeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly OrganizationId OrgId = OrganizationId.New();
    private const string Code = "ABCD1234";

    private static PeekProvisioningCodeQueryHandler BuildHandler(
        TestProvisioningCodeRepoForPeek? codeRepo = null,
        TestOrgRepoForPeek? orgRepo = null)
    {
        orgRepo ??= DefaultOrgRepo();
        return new PeekProvisioningCodeQueryHandler(
            codeRepo ?? DefaultCodeRepo(),
            orgRepo,
            new TestClockForPeek(Now));
    }

    private static TestOrgRepoForPeek DefaultOrgRepo()
    {
        var repo = new TestOrgRepoForPeek();
        repo.Add(Organization.Create(OrgId, "Test Org", "SI00000001", "0000001000",
            TimeSpan.FromDays(7), Now));
        return repo;
    }

    private static TestProvisioningCodeRepoForPeek DefaultCodeRepo()
    {
        var repo = new TestProvisioningCodeRepoForPeek();
        repo.Add(IssueCode());
        return repo;
    }

    private static ProvisioningCode IssueCode(bool expired = false, bool redeemed = false)
    {
        var expiresAt = expired ? Now.AddDays(-1) : Now.AddHours(24);
        var issuedAt = expired ? Now.AddDays(-20) : Now;

        var code = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrgId,
            Code,
            "Ana",
            "Kovač",
            "ana@example.com",
            new[] { EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            OrganizationAdminAccountId.New(),
            issuedAt,
            expiresAt,
            null);

        if (redeemed)
            code.Redeem(issuedAt.AddMinutes(5), EmployeeAccountId.New());

        return code;
    }

    [Fact]
    public async Task Handle_returns_pre_filled_data()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(new PeekProvisioningCodeQuery(Code), CancellationToken.None);

        result.FirstName.Should().Be("Ana");
        result.LastName.Should().Be("Kovač");
        result.Email.Should().Be("ana@example.com");
        result.Roles.Should().ContainSingle(r => r == "Operator");
        result.OrganizationId.Should().Be(OrgId.Value);
        result.OrganizationName.Should().Be("Test Org");
        result.ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Handle_with_unknown_code_throws_not_found()
    {
        var handler = BuildHandler();
        var act = () => handler.Handle(new PeekProvisioningCodeQuery("UNKNOWN1"), CancellationToken.None);
        await act.Should().ThrowAsync<ProvisioningCodeNotFoundException>();
    }

    [Fact]
    public async Task Handle_with_expired_code_throws_expired()
    {
        var repo = new TestProvisioningCodeRepoForPeek();
        repo.Add(IssueCode(expired: true));
        var handler = BuildHandler(codeRepo: repo);

        var act = () => handler.Handle(new PeekProvisioningCodeQuery(Code), CancellationToken.None);
        await act.Should().ThrowAsync<ProvisioningCodeExpiredException>();
    }

    [Fact]
    public async Task Handle_with_redeemed_code_throws_already_redeemed()
    {
        var repo = new TestProvisioningCodeRepoForPeek();
        repo.Add(IssueCode(redeemed: true));
        var handler = BuildHandler(codeRepo: repo);

        var act = () => handler.Handle(new PeekProvisioningCodeQuery(Code), CancellationToken.None);
        await act.Should().ThrowAsync<ProvisioningCodeAlreadyRedeemedException>();
    }
}

public sealed class TestProvisioningCodeRepoForPeek : IProvisioningCodeRepository
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

public sealed class TestOrgRepoForPeek : IOrganizationRepository
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

public sealed class TestClockForPeek : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForPeek(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}
