using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class CitizenUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly byte[] AnyPublicKey = new byte[] { 1, 2, 3, 4 };

    [Fact]
    public void Onboard_creates_citizen_and_raises_event()
    {
        var id = CitizenUserId.New();
        var citizen = CitizenUser.Onboard(
            id,
            firstName: "Janez",
            lastName: "Novak",
            emso: "0101000500001",
            email: "janez@example.com",
            phoneNumber: "+38640123456",
            now: Now);

        citizen.Id.Should().Be(id);
        citizen.FirstName.Should().Be("Janez");
        citizen.LastName.Should().Be("Novak");
        citizen.Emso.Should().Be("0101000500001");
        citizen.Email.Should().Be("janez@example.com");
        citizen.PhoneNumber.Should().Be("+38640123456");
        citizen.OnboardedAt.Should().Be(Now);
        citizen.Devices.Should().BeEmpty();
        citizen.DomainEvents.OfType<CitizenOnboarded>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Onboard_with_blank_first_name_throws(string firstName)
    {
        var act = () => CitizenUser.Onboard(CitizenUserId.New(), firstName, "n", "0101000500001", null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("firstName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Onboard_with_blank_last_name_throws(string lastName)
    {
        var act = () => CitizenUser.Onboard(CitizenUserId.New(), "n", lastName, "0101000500001", null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("lastName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("01010005000010")]
    [InlineData("0101000500a01")]
    public void Onboard_with_invalid_emso_throws(string emso)
    {
        var act = () => CitizenUser.Onboard(CitizenUserId.New(), "n", "l", emso, null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("emso");
    }

    [Fact]
    public void RegisterDevice_appends_active_device_and_raises_event()
    {
        var citizen = ValidCitizen();
        var deviceId = CitizenDeviceId.New();

        var device = citizen.RegisterDevice(deviceId, AnyPublicKey, "fp", "iPhone 14", Now.AddMinutes(1));

        device.Id.Should().Be(deviceId);
        device.CitizenUserId.Should().Be(citizen.Id);
        device.PublicKey.Should().BeEquivalentTo(AnyPublicKey);
        device.DeviceFingerprint.Should().Be("fp");
        device.Label.Should().Be("iPhone 14");
        device.RegisteredAt.Should().Be(Now.AddMinutes(1));
        device.RevokedAt.Should().BeNull();
        device.IsActive.Should().BeTrue();

        citizen.Devices.Should().ContainSingle().Which.Should().BeSameAs(device);
        citizen.DomainEvents.OfType<CitizenDeviceRegistered>().Should().ContainSingle();
    }

    [Fact]
    public void RegisterDevice_allows_multiple_active_devices()
    {
        var citizen = ValidCitizen();
        citizen.RegisterDevice(CitizenDeviceId.New(), AnyPublicKey, "fp1", null, Now);
        citizen.RegisterDevice(CitizenDeviceId.New(), AnyPublicKey, "fp2", null, Now);

        citizen.Devices.Should().HaveCount(2).And.OnlyContain(d => d.IsActive);
    }

    [Fact]
    public void RegisterDevice_with_empty_public_key_throws()
    {
        var citizen = ValidCitizen();
        var act = () => citizen.RegisterDevice(CitizenDeviceId.New(), Array.Empty<byte>(), "fp", null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("publicKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterDevice_with_blank_fingerprint_throws(string fingerprint)
    {
        var citizen = ValidCitizen();
        var act = () => citizen.RegisterDevice(CitizenDeviceId.New(), AnyPublicKey, fingerprint, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("deviceFingerprint");
    }

    [Fact]
    public void RevokeDevice_sets_RevokedAt_and_raises_event()
    {
        var citizen = ValidCitizen();
        var deviceId = CitizenDeviceId.New();
        citizen.RegisterDevice(deviceId, AnyPublicKey, "fp", null, Now);

        citizen.RevokeDevice(deviceId, Now.AddDays(1));

        var device = citizen.Devices.Single();
        device.RevokedAt.Should().Be(Now.AddDays(1));
        device.IsActive.Should().BeFalse();
        citizen.DomainEvents.OfType<CitizenDeviceRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void RevokeDevice_unknown_id_throws()
    {
        var citizen = ValidCitizen();
        var act = () => citizen.RevokeDevice(CitizenDeviceId.New(), Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*device not found*");
    }

    [Fact]
    public void RevokeDevice_already_revoked_throws()
    {
        var citizen = ValidCitizen();
        var id = CitizenDeviceId.New();
        citizen.RegisterDevice(id, AnyPublicKey, "fp", null, Now);
        citizen.RevokeDevice(id, Now.AddDays(1));

        var act = () => citizen.RevokeDevice(id, Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already revoked*");
    }

    private static CitizenUser ValidCitizen() =>
        CitizenUser.Onboard(CitizenUserId.New(), "Janez", "Novak", "0101000500001", null, null, Now);
}
