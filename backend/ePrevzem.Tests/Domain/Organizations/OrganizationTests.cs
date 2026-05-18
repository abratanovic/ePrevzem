using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Organizations.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Organizations;

public class OrganizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_with_valid_fields_constructs_aggregate_and_raises_event()
    {
        var id = OrganizationId.New();
        var org = Organization.Create(
            id,
            name: "Notarska zbornica",
            taxNumber: "SI12345678",
            registrationNumber: "1234567000",
            defaultPickupDuration: TimeSpan.FromDays(5),
            now: Now);

        org.Id.Should().Be(id);
        org.Name.Should().Be("Notarska zbornica");
        org.TaxNumber.Should().Be("SI12345678");
        org.RegistrationNumber.Should().Be("1234567000");
        org.DefaultPickupDuration.Should().Be(TimeSpan.FromDays(5));
        org.CreatedAt.Should().Be(Now);
        org.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrganizationCreated>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_name_throws(string name)
    {
        var act = () => Organization.Create(OrganizationId.New(), name, "SI1", "1", TimeSpan.FromDays(1), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_tax_number_throws(string taxNumber)
    {
        var act = () => Organization.Create(OrganizationId.New(), "n", taxNumber, "1", TimeSpan.FromDays(1), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("taxNumber");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_registration_number_throws(string registrationNumber)
    {
        var act = () => Organization.Create(OrganizationId.New(), "n", "SI1", registrationNumber, TimeSpan.FromDays(1), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("registrationNumber");
    }

    [Fact]
    public void Create_with_non_positive_pickup_duration_throws()
    {
        var act = () => Organization.Create(OrganizationId.New(), "n", "SI1", "1", TimeSpan.Zero, Now);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("defaultPickupDuration");
    }

    [Fact]
    public void ChangeDefaultPickupDuration_updates_value()
    {
        var org = Organization.Create(OrganizationId.New(), "n", "SI1", "1", TimeSpan.FromDays(5), Now);
        org.ChangeDefaultPickupDuration(TimeSpan.FromDays(10));
        org.DefaultPickupDuration.Should().Be(TimeSpan.FromDays(10));
    }

    [Fact]
    public void ChangeDefaultPickupDuration_with_non_positive_throws()
    {
        var org = Organization.Create(OrganizationId.New(), "n", "SI1", "1", TimeSpan.FromDays(5), Now);
        var act = () => org.ChangeDefaultPickupDuration(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
