using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity;

public sealed class OrganizationAdminAccount : AggregateRoot<OrganizationAdminAccountId>
{
    public OrganizationId OrganizationId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool MustChangePassword { get; private set; }
    public OrganizationAdminAccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private OrganizationAdminAccount() { }

    public static OrganizationAdminAccount Create(
        OrganizationAdminAccountId id,
        OrganizationId organizationId,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        var account = new OrganizationAdminAccount
        {
            Id = id,
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            MustChangePassword = true,
            Status = OrganizationAdminAccountStatus.Active,
            CreatedAt = now
        };
        account.Raise(new OrganizationAdminAccountCreated(id, organizationId, now));
        return account;
    }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        MustChangePassword = false;
        Raise(new OrganizationAdminPasswordChanged(Id, now));
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        Raise(new OrganizationAdminLoggedIn(Id, now));
    }

    public void Disable(DateTimeOffset now)
    {
        if (Status == OrganizationAdminAccountStatus.Disabled)
            throw new InvalidOperationException("Account is already disabled.");
        Status = OrganizationAdminAccountStatus.Disabled;
        Raise(new OrganizationAdminAccountDisabled(Id, now));
    }

    public void Reenable(DateTimeOffset now)
    {
        if (Status == OrganizationAdminAccountStatus.Active)
            throw new InvalidOperationException("Account is already active.");
        Status = OrganizationAdminAccountStatus.Active;
        Raise(new OrganizationAdminAccountReenabled(Id, now));
    }
}
