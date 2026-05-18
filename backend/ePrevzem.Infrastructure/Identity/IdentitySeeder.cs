using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ePrevzem.Infrastructure.Identity;

public sealed class IdentitySeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IdentityOptions> _options;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> options,
        ILogger<IdentitySeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var systemAdminRepository = scope.ServiceProvider.GetRequiredService<ISystemAdminRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await systemAdminRepository.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Skipping identity bootstrap because a system admin already exists.");
            return;
        }

        var username = _options.Value.BootstrapAdmin.Username;
        var initialPassword = _options.Value.BootstrapAdmin.InitialPassword;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(initialPassword))
        {
            throw new InvalidOperationException(
                "No system admin exists. Configure Identity:BootstrapAdmin:Username and Identity:BootstrapAdmin:InitialPassword via user-secrets or environment variables.");
        }

        var admin = SystemAdmin.Create(
            SystemAdminId.New(),
            username,
            passwordHasher.Hash(initialPassword),
            clock.UtcNow);

        await systemAdminRepository.AddAsync(admin, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Bootstrap system admin account was created.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
