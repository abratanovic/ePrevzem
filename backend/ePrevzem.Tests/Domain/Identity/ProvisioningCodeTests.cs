using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class ProvisioningCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_creates_unredeemed_code_with_event()
    {
        var id = ProvisioningCodeId.New();
        var orgId = OrganizationId.New();
        var roles = new[] { EmployeeAccountRole.Operator };
        var creator = OrganizationAdminAccountId.New();

        var code = ProvisioningCode.Issue(
            id,
            orgId,
            code: "ABCD-1234",
            preFilledInfo: PersonalInfo.Create("Ana", "Kovač", "ana@example.com"),
            roles: roles,
            createdBy: creator,
            now: Now,
            expiresAt: Now.AddHours(24),
            isReprovisioningOf: null);

        code.Id.Should().Be(id);
        code.OrganizationId.Should().Be(orgId);
        code.Code.Should().Be("ABCD-1234");
        code.PreFilledInfo.FirstName.Should().Be("Ana");
        code.PreFilledInfo.LastName.Should().Be("Kovač");
        code.PreFilledInfo.Email.Should().Be("ana@example.com");
        code.Roles.Should().BeEquivalentTo(roles);
        code.CreatedByOrganizationAdminId.Should().Be(creator);
        code.CreatedAt.Should().Be(Now);
        code.ExpiresAt.Should().Be(Now.AddHours(24));
        code.RedeemedAt.Should().BeNull();
        code.IsReprovisioningOfEmployeeAccountId.Should().BeNull();
        code.IsRedeemable(at: Now).Should().BeTrue();
        code.DomainEvents.OfType<ProvisioningCodeIssued>().Should().ContainSingle();
    }

    [Fact]
    public void Issue_with_empty_roles_throws()
    {
        var act = () => ProvisioningCode.Issue(
            ProvisioningCodeId.New(), OrganizationId.New(), "C", PersonalInfo.Create("F", "L", null),
            Array.Empty<EmployeeAccountRole>(),
            OrganizationAdminAccountId.New(), Now, Now.AddHours(1), null);
        act.Should().Throw<ArgumentException>().WithParameterName("roles");
    }

    [Fact]
    public void Issue_with_expiration_in_past_throws()
    {
        var act = () => ProvisioningCode.Issue(
            ProvisioningCodeId.New(), OrganizationId.New(), "C", PersonalInfo.Create("F", "L", null),
            new[] { EmployeeAccountRole.Operator },
            OrganizationAdminAccountId.New(), Now, Now.AddMinutes(-1), null);
        act.Should().Throw<ArgumentException>().WithParameterName("expiresAt");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_with_blank_code_throws(string code)
    {
        var act = () => ProvisioningCode.Issue(
            ProvisioningCodeId.New(), OrganizationId.New(), code, PersonalInfo.Create("F", "L", null),
            new[] { EmployeeAccountRole.Operator },
            OrganizationAdminAccountId.New(), Now, Now.AddHours(1), null);
        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void Redeem_sets_redemption_state_and_raises_event()
    {
        var code = Issue();
        var newAccount = EmployeeAccountId.New();

        code.Redeem(redeemedAt: Now.AddMinutes(5), redeemedIntoEmployeeAccountId: newAccount);

        code.RedeemedAt.Should().Be(Now.AddMinutes(5));
        code.RedeemedIntoEmployeeAccountId.Should().Be(newAccount);
        code.IsRedeemable(at: Now.AddMinutes(5)).Should().BeFalse();
        code.DomainEvents.OfType<ProvisioningCodeRedeemed>().Should().ContainSingle();
    }

    [Fact]
    public void Redeem_twice_throws()
    {
        var code = Issue();
        code.Redeem(Now.AddMinutes(1), EmployeeAccountId.New());

        var act = () => code.Redeem(Now.AddMinutes(2), EmployeeAccountId.New());
        act.Should().Throw<InvalidOperationException>().WithMessage("*already redeemed*");
    }

    [Fact]
    public void Redeem_after_expiry_throws()
    {
        var code = Issue();
        var act = () => code.Redeem(Now.AddHours(2), EmployeeAccountId.New());
        act.Should().Throw<InvalidOperationException>().WithMessage("*expired*");
    }

    [Fact]
    public void IsRedeemable_is_false_after_expiry()
    {
        var code = Issue();
        code.IsRedeemable(Now.AddHours(2)).Should().BeFalse();
    }

    private static ProvisioningCode Issue(
        EmployeeAccountId? reprovisioningOf = null)
        => ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrganizationId.New(),
            code: "ABCD-1234",
            preFilledInfo: PersonalInfo.Create("Ana", "Kovač", null),
            roles: new[] { EmployeeAccountRole.Operator },
            createdBy: OrganizationAdminAccountId.New(),
            now: Now,
            expiresAt: Now.AddHours(1),
            isReprovisioningOf: reprovisioningOf);
}
