using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;

namespace ePrevzem.Domain.Identity;

public sealed class CitizenUser : AggregateRoot<CitizenUserId>
{
    private readonly List<CitizenDevice> _devices = new();

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Emso { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTimeOffset OnboardedAt { get; private set; }
    public IReadOnlyCollection<CitizenDevice> Devices => _devices.AsReadOnly();

    private CitizenUser() { }

    public static CitizenUser Onboard(
        CitizenUserId id,
        string firstName,
        string lastName,
        string emso,
        string? email,
        string? phoneNumber,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (!IsValidEmso(emso))
            throw new ArgumentException("EMSO must be 13 digits.", nameof(emso));

        var user = new CitizenUser
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Emso = emso,
            Email = email,
            PhoneNumber = phoneNumber,
            OnboardedAt = now
        };
        user.Raise(new CitizenOnboarded(id, now));
        return user;
    }

    public CitizenDevice RegisterDevice(
        CitizenDeviceId id,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset now)
    {
        var device = CitizenDevice.Register(id, Id, publicKey, deviceFingerprint, label, now);
        _devices.Add(device);
        Raise(new CitizenDeviceRegistered(Id, id, now));
        return device;
    }

    public void RevokeDevice(CitizenDeviceId deviceId, DateTimeOffset now)
    {
        var device = _devices.SingleOrDefault(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("Citizen device not found on this user.");
        device.Revoke(now);
        Raise(new CitizenDeviceRevoked(Id, deviceId, now));
    }

    private static bool IsValidEmso(string? emso)
        => !string.IsNullOrWhiteSpace(emso) && emso.Length == 13 && emso.All(char.IsDigit);
}
