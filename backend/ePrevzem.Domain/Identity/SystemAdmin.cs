using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class SystemAdmin : AggregateRoot<SystemAdminId>
{
    public string Username { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private SystemAdmin() { }

    public static SystemAdmin Create(SystemAdminId id, string username, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        return new SystemAdmin { Id = id, Username = username, CreatedAt = now };
    }
}
