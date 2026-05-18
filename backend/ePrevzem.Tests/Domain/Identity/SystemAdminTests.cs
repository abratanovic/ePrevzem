using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class SystemAdminTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_constructs_system_admin_with_password_hash()
    {
        var id = SystemAdminId.New();
        var passwordHash = "pbkdf2_hash";
        var admin = SystemAdmin.Create(id, "ops-jane", passwordHash, Now);

        admin.Id.Should().Be(id);
        admin.Username.Should().Be("ops-jane");
        admin.PasswordHash.Should().Be(passwordHash);
        admin.CreatedAt.Should().Be(Now);
        admin.LastLoginAt.Should().BeNull();
        admin.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_normalizes_username_to_lowercase_and_trimmed()
    {
        var id = SystemAdminId.New();
        var admin = SystemAdmin.Create(id, "  OPS-JANE  ", "hash", Now);

        admin.Username.Should().Be("ops-jane");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_username_throws(string username)
    {
        var act = () => SystemAdmin.Create(SystemAdminId.New(), username, "hash", Now);
        act.Should().Throw<ArgumentException>().WithParameterName("username");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_password_hash_throws(string passwordHash)
    {
        var act = () => SystemAdmin.Create(SystemAdminId.New(), "ops-jane", passwordHash, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("passwordHash");
    }

    [Fact]
    public void SetPassword_updates_password_hash_and_raises_event()
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "old_hash", Now);
        admin.ClearDomainEvents();

        var newHash = "new_pbkdf2_hash";
        var laterTime = Now.AddHours(1);
        admin.SetPassword(newHash, laterTime);

        admin.PasswordHash.Should().Be(newHash);
        admin.DomainEvents.OfType<SystemAdminPasswordChanged>().Should().ContainSingle()
            .Which.Should().Match<SystemAdminPasswordChanged>(e =>
                e.SystemAdminId == admin.Id &&
                e.OccurredOn == laterTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPassword_with_blank_hash_throws(string passwordHash)
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);

        var act = () => admin.SetPassword(passwordHash, Now.AddHours(1));
        act.Should().Throw<ArgumentException>().WithParameterName("passwordHash");
    }

    [Fact]
    public void RecordLogin_sets_LastLoginAt_and_raises_event()
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);
        admin.ClearDomainEvents();
        admin.LastLoginAt.Should().BeNull();

        var loginTime = Now.AddHours(2);
        admin.RecordLogin(loginTime);

        admin.LastLoginAt.Should().Be(loginTime);
        admin.DomainEvents.OfType<SystemAdminLoggedIn>().Should().ContainSingle()
            .Which.Should().Match<SystemAdminLoggedIn>(e =>
                e.SystemAdminId == admin.Id &&
                e.OccurredOn == loginTime);
    }

    [Fact]
    public void RecordLogin_multiple_times_updates_to_latest()
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);
        var firstLogin = Now.AddHours(1);
        admin.RecordLogin(firstLogin);

        admin.LastLoginAt.Should().Be(firstLogin);

        var secondLogin = Now.AddHours(2);
        admin.RecordLogin(secondLogin);

        admin.LastLoginAt.Should().Be(secondLogin);
        admin.DomainEvents.OfType<SystemAdminLoggedIn>().Should().HaveCount(2);
    }
}
