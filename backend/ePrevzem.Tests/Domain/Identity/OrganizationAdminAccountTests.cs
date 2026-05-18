using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class OrganizationAdminAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_constructs_account_with_must_change_password_and_raises_event()
    {
        var id = OrganizationAdminAccountId.New();
        var orgId = OrganizationId.New();

        var account = OrganizationAdminAccount.Create(id, orgId, "Ana", "Kovač", "ana@example.com", "hash", Now);

        account.Id.Should().Be(id);
        account.OrganizationId.Should().Be(orgId);
        account.FirstName.Should().Be("Ana");
        account.LastName.Should().Be("Kovač");
        account.Email.Should().Be("ana@example.com");
        account.PasswordHash.Should().Be("hash");
        account.MustChangePassword.Should().BeTrue();
        account.Status.Should().Be(OrganizationAdminAccountStatus.Active);
        account.CreatedAt.Should().Be(Now);
        account.LastLoginAt.Should().BeNull();
        account.DomainEvents.OfType<OrganizationAdminAccountCreated>().Should().ContainSingle()
            .Which.Should().Match<OrganizationAdminAccountCreated>(e =>
                e.OrganizationAdminAccountId == id &&
                e.OrganizationId == orgId &&
                e.OccurredOn == Now);
    }

    [Fact]
    public void Create_normalizes_email_to_lowercase_and_trimmed()
    {
        var account = OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", "  ANA@Example.COM  ", "hash", Now);

        account.Email.Should().Be("ana@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_first_name_throws(string firstName)
    {
        var act = () => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            firstName, "Kovač", "ana@example.com", "hash", Now);
        act.Should().Throw<ArgumentException>().WithParameterName("firstName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_last_name_throws(string lastName)
    {
        var act = () => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", lastName, "ana@example.com", "hash", Now);
        act.Should().Throw<ArgumentException>().WithParameterName("lastName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_email_throws(string email)
    {
        var act = () => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", email, "hash", Now);
        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_password_hash_throws(string passwordHash)
    {
        var act = () => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", "ana@example.com", passwordHash, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("passwordHash");
    }

    [Fact]
    public void SetPassword_updates_hash_clears_must_change_password_and_raises_event()
    {
        var account = MakeAccount();
        account.ClearDomainEvents();

        account.SetPassword("new_hash", Now.AddHours(1));

        account.PasswordHash.Should().Be("new_hash");
        account.MustChangePassword.Should().BeFalse();
        account.DomainEvents.OfType<OrganizationAdminPasswordChanged>().Should().ContainSingle()
            .Which.Should().Match<OrganizationAdminPasswordChanged>(e =>
                e.OrganizationAdminAccountId == account.Id &&
                e.OccurredOn == Now.AddHours(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPassword_with_blank_hash_throws(string passwordHash)
    {
        var account = MakeAccount();
        var act = () => account.SetPassword(passwordHash, Now.AddHours(1));
        act.Should().Throw<ArgumentException>().WithParameterName("passwordHash");
    }

    [Fact]
    public void RecordLogin_sets_last_login_and_raises_event()
    {
        var account = MakeAccount();
        account.ClearDomainEvents();

        account.RecordLogin(Now.AddHours(1));

        account.LastLoginAt.Should().Be(Now.AddHours(1));
        account.DomainEvents.OfType<OrganizationAdminLoggedIn>().Should().ContainSingle()
            .Which.Should().Match<OrganizationAdminLoggedIn>(e =>
                e.OrganizationAdminAccountId == account.Id &&
                e.OccurredOn == Now.AddHours(1));
    }

    [Fact]
    public void Disable_sets_status_and_raises_event()
    {
        var account = MakeAccount();
        account.ClearDomainEvents();

        account.Disable(Now.AddDays(1));

        account.Status.Should().Be(OrganizationAdminAccountStatus.Disabled);
        account.DomainEvents.OfType<OrganizationAdminAccountDisabled>().Should().ContainSingle();
    }

    [Fact]
    public void Disable_when_already_disabled_throws()
    {
        var account = MakeAccount();
        account.Disable(Now.AddDays(1));

        var act = () => account.Disable(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already disabled*");
    }

    [Fact]
    public void Reenable_restores_status_and_raises_event()
    {
        var account = MakeAccount();
        account.Disable(Now.AddDays(1));
        account.ClearDomainEvents();

        account.Reenable(Now.AddDays(2));

        account.Status.Should().Be(OrganizationAdminAccountStatus.Active);
        account.DomainEvents.OfType<OrganizationAdminAccountReenabled>().Should().ContainSingle();
    }

    [Fact]
    public void Reenable_when_already_active_throws()
    {
        var account = MakeAccount();
        var act = () => account.Reenable(Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already active*");
    }

    private static OrganizationAdminAccount MakeAccount()
        => OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(), OrganizationId.New(),
            "Ana", "Kovač", "ana@example.com", "hash", Now);
}
