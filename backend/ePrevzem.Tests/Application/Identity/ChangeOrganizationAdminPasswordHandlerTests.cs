using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.ChangeOrgAdminPassword;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class ChangeOrganizationAdminPasswordHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private const string CurrentPassword = "OldPass1!";
    private const string NewPassword = "NewPass2!";
    private const string CurrentHash = "hashed:OldPass1!";

    private static ChangeOrganizationAdminPasswordCommandHandler BuildHandler(
        TestOrgAdminRepoForChangePassword? repo = null,
        TestPasswordHasherForChangePassword? hasher = null,
        TestUnitOfWorkForChangePassword? uow = null)
        => new(
            repo ?? DefaultRepo(),
            hasher ?? new TestPasswordHasherForChangePassword(),
            uow ?? new TestUnitOfWorkForChangePassword(),
            new TestClockForChangePassword(Now));

    private static OrganizationAdminAccount MakeAccount()
        => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", "ana@example.com", CurrentHash, Now);

    private static TestOrgAdminRepoForChangePassword DefaultRepo()
    {
        var repo = new TestOrgAdminRepoForChangePassword();
        repo.Add(MakeAccount());
        return repo;
    }

    [Fact]
    public async Task Handle_clears_must_change_password_flag()
    {
        var repo = new TestOrgAdminRepoForChangePassword();
        var account = MakeAccount();
        repo.Add(account);
        var handler = BuildHandler(repo: repo);

        await handler.Handle(
            new ChangeOrganizationAdminPasswordCommand(account.Id.Value, CurrentPassword, NewPassword),
            CancellationToken.None);

        account.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_updates_password_hash()
    {
        var repo = new TestOrgAdminRepoForChangePassword();
        var account = MakeAccount();
        repo.Add(account);
        var handler = BuildHandler(repo: repo);

        await handler.Handle(
            new ChangeOrganizationAdminPasswordCommand(account.Id.Value, CurrentPassword, NewPassword),
            CancellationToken.None);

        account.PasswordHash.Should().Be("hashed:NewPass2!");
    }

    [Fact]
    public async Task Handle_saves_changes()
    {
        var uow = new TestUnitOfWorkForChangePassword();
        var repo = new TestOrgAdminRepoForChangePassword();
        var account = MakeAccount();
        repo.Add(account);
        var handler = BuildHandler(repo: repo, uow: uow);

        await handler.Handle(
            new ChangeOrganizationAdminPasswordCommand(account.Id.Value, CurrentPassword, NewPassword),
            CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_wrong_current_password_throws()
    {
        var repo = new TestOrgAdminRepoForChangePassword();
        var account = MakeAccount();
        repo.Add(account);
        var handler = BuildHandler(repo: repo);

        var act = () => handler.Handle(
            new ChangeOrganizationAdminPasswordCommand(account.Id.Value, "WrongPassword!", NewPassword),
            CancellationToken.None);

        await act.Should().ThrowAsync<WrongCurrentPasswordException>();
    }

    [Fact]
    public async Task Handle_with_wrong_current_password_does_not_update_hash()
    {
        var repo = new TestOrgAdminRepoForChangePassword();
        var account = MakeAccount();
        repo.Add(account);
        var handler = BuildHandler(repo: repo);

        try
        {
            await handler.Handle(
                new ChangeOrganizationAdminPasswordCommand(account.Id.Value, "WrongPassword!", NewPassword),
                CancellationToken.None);
        }
        catch (WrongCurrentPasswordException) { }

        account.PasswordHash.Should().Be(CurrentHash);
    }
}

public sealed class TestOrgAdminRepoForChangePassword : IOrganizationAdminAccountRepository
{
    private readonly List<OrganizationAdminAccount> _items = new();

    public void Add(OrganizationAdminAccount account) => _items.Add(account);

    public Task<OrganizationAdminAccount?> GetByIdAsync(OrganizationAdminAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

    public Task<OrganizationAdminAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(x => x.Email == normalizedEmail));

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.Any(x => x.Email == normalizedEmail));

    public Task AddAsync(OrganizationAdminAccount account, CancellationToken cancellationToken = default)
    {
        _items.Add(account);
        return Task.CompletedTask;
    }
}

public sealed class TestPasswordHasherForChangePassword : IPasswordHasher
{
    public string Hash(string plaintext) => $"hashed:{plaintext}";
    public PasswordVerification Verify(string hash, string plaintext)
        => hash == $"hashed:{plaintext}" ? PasswordVerification.Success : PasswordVerification.Failed;
}

public sealed class TestClockForChangePassword : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForChangePassword(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForChangePassword : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}
