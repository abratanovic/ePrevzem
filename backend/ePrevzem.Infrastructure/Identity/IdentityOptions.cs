namespace ePrevzem.Infrastructure.Identity;

public sealed class IdentityOptions
{
    public BootstrapAdminOptions BootstrapAdmin { get; init; } = new();
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    public int RefreshTokenLifetimeDays { get; init; } = 14;
}

public sealed class BootstrapAdminOptions
{
    public string Username { get; init; } = string.Empty;
    public string InitialPassword { get; init; } = string.Empty;
}
