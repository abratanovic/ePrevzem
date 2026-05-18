using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.CreateOrgAdmin;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class CreateOrganizationAdminHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly OrganizationId OrgId = OrganizationId.New();

    private static CreateOrganizationAdminCommandHandler BuildHandler(
        TestOrganizationAdminAccountRepository? repo = null,
        TestOrgRepositoryForAdminTests? orgRepo = null,
        TestPasswordHasherForAdminTests? hasher = null,
        TestUnitOfWorkForAdminTests? uow = null)
    {
        var defaultOrgRepo = orgRepo ?? new TestOrgRepositoryForAdminTests();
        defaultOrgRepo.AddOrg(OrgId);

        return new CreateOrganizationAdminCommandHandler(
            repo ?? new TestOrganizationAdminAccountRepository(),
            defaultOrgRepo,
            hasher ?? new TestPasswordHasherForAdminTests(),
            uow ?? new TestUnitOfWorkForAdminTests(),
            new TestClockForAdminTests(Now));
    }

    private static CreateOrganizationAdminCommand ValidCommand(
        string email = "ana@example.com",
        Guid? orgId = null)
        => new("Ana", "Kovač", email, orgId ?? OrgId.Value);

    [Fact]
    public async Task Handle_returns_response_with_id_email_and_org()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.Email.Should().Be("ana@example.com");
        result.OrganizationId.Should().Be(OrgId.Value);
    }

    [Fact]
    public async Task Handle_returns_non_empty_temporary_password()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.TemporaryPassword.Should().NotBeNullOrWhiteSpace();
        result.TemporaryPassword.Should().HaveLength(12);
    }

    [Fact]
    public async Task Handle_persists_account_with_hashed_password()
    {
        var repo = new TestOrganizationAdminAccountRepository();
        var hasher = new TestPasswordHasherForAdminTests();
        var handler = BuildHandler(repo: repo, hasher: hasher);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        repo.Items.Should().HaveCount(1);
        repo.Items[0].PasswordHash.Should().Be($"hashed:{result.TemporaryPassword}");
        repo.Items[0].MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_normalizes_email_to_lowercase()
    {
        var repo = new TestOrganizationAdminAccountRepository();
        var handler = BuildHandler(repo: repo);

        await handler.Handle(ValidCommand(email: "ANA@Example.COM"), CancellationToken.None);

        repo.Items[0].Email.Should().Be("ana@example.com");
    }

    [Fact]
    public async Task Handle_saves_changes()
    {
        var uow = new TestUnitOfWorkForAdminTests();
        var handler = BuildHandler(uow: uow);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_duplicate_email_throws()
    {
        var repo = new TestOrganizationAdminAccountRepository();
        repo.AddExistingEmail("ana@example.com");
        var handler = BuildHandler(repo: repo);

        var act = () => handler.Handle(ValidCommand(email: "ana@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateOrgAdminEmailException>();
    }

    [Fact]
    public async Task Handle_with_duplicate_email_does_not_persist()
    {
        var repo = new TestOrganizationAdminAccountRepository();
        repo.AddExistingEmail("ana@example.com");
        var handler = BuildHandler(repo: repo);

        try { await handler.Handle(ValidCommand(email: "ana@example.com"), CancellationToken.None); }
        catch (DuplicateOrgAdminEmailException) { }

        repo.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_with_unknown_organization_throws()
    {
        var orgRepo = new TestOrgRepositoryForAdminTests();
        var handler = BuildHandler(orgRepo: orgRepo);

        var act = () => handler.Handle(ValidCommand(orgId: Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<OrganizationNotFoundException>();
    }
}

public sealed class TestOrganizationAdminAccountRepository : IOrganizationAdminAccountRepository
{
    public List<OrganizationAdminAccount> Items { get; } = new();
    private readonly HashSet<string> _existingEmails = new();

    public void AddExistingEmail(string email) => _existingEmails.Add(email);

    public Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<OrganizationAdminAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Email == normalizedEmail));

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_existingEmails.Contains(normalizedEmail) || Items.Any(x => x.Email == normalizedEmail));

    public Task AddAsync(OrganizationAdminAccount account, CancellationToken cancellationToken = default)
    {
        Items.Add(account);
        return Task.CompletedTask;
    }
}

public sealed class TestOrgRepositoryForAdminTests : IOrganizationRepository
{
    private readonly List<Organization> _orgs = new();

    public void AddOrg(OrganizationId id)
    {
        var org = Organization.Create(id, "Test Org", "SI00000001", "0000001000", TimeSpan.FromDays(7),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _orgs.Add(org);
    }

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

public sealed class TestPasswordHasherForAdminTests : IPasswordHasher
{
    public string Hash(string plaintext) => $"hashed:{plaintext}";
    public PasswordVerification Verify(string hash, string plaintext)
        => hash == $"hashed:{plaintext}" ? PasswordVerification.Success : PasswordVerification.Failed;
}

public sealed class TestClockForAdminTests : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForAdminTests(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForAdminTests : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}
