using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;

namespace ePrevzem.Domain.Identity;

public sealed class SystemAdmin : AggregateRoot<SystemAdminId>
{
    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private SystemAdmin() { }

    public static SystemAdmin Create(SystemAdminId id, string username, string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new SystemAdmin
        {
            Id = id,
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = now
        };
    }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        Raise(new SystemAdminPasswordChanged(Id, now));
    }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        Raise(new SystemAdminLoggedIn(Id, now));
    }
}
