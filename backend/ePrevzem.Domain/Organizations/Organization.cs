using ePrevzem.Domain.Common;
using ePrevzem.Domain.Organizations.Events;

namespace ePrevzem.Domain.Organizations;

public sealed class Organization : AggregateRoot<OrganizationId>
{
    public string Name { get; private set; } = default!;
    public string TaxNumber { get; private set; } = default!;
    public string RegistrationNumber { get; private set; } = default!;
    public TimeSpan DefaultPickupDuration { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Organization() { }

    public static Organization Create(
        OrganizationId id,
        string name,
        string taxNumber,
        string registrationNumber,
        TimeSpan defaultPickupDuration,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(taxNumber))
            throw new ArgumentException("Tax number is required.", nameof(taxNumber));
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number is required.", nameof(registrationNumber));
        if (defaultPickupDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(defaultPickupDuration), defaultPickupDuration, "Default pickup duration must be positive.");

        var org = new Organization
        {
            Id = id,
            Name = name,
            TaxNumber = taxNumber,
            RegistrationNumber = registrationNumber,
            DefaultPickupDuration = defaultPickupDuration,
            CreatedAt = now
        };
        org.Raise(new OrganizationCreated(id, now));
        return org;
    }

    public void ChangeDefaultPickupDuration(TimeSpan newDuration)
    {
        if (newDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(newDuration), newDuration, "Default pickup duration must be positive.");
        DefaultPickupDuration = newDuration;
    }
}
