using ePrevzem.Domain.Identity;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class SystemAdminTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_constructs_system_admin()
    {
        var id = SystemAdminId.New();
        var admin = SystemAdmin.Create(id, "ops-jane", Now);

        admin.Id.Should().Be(id);
        admin.Username.Should().Be("ops-jane");
        admin.CreatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_username_throws(string username)
    {
        var act = () => SystemAdmin.Create(SystemAdminId.New(), username, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("username");
    }
}
