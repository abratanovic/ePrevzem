using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Infrastructure.Identity;
using ePrevzem.Infrastructure.Persistence;
using ePrevzem.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ePrevzem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ePrevzem")
            ?? throw new InvalidOperationException("ConnectionStrings:ePrevzem is not configured");

        services.Configure<IdentityOptions>(configuration.GetSection("Identity"));
        services.Configure<JwtTokenOptions>(configuration.GetSection("Jwt"));

        services.AddDbContext<EPrevzemDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EPrevzemDbContext>());
        services.AddScoped<ISystemAdminRepository, SystemAdminRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddHostedService<IdentitySeeder>();

        // Outbound ports — adapters wired here when implemented.
        // services.AddHttpClient<ISiTrustClient, SiTrustClient>(...);
        // services.AddHttpClient<ILockerGateway, Direct4MeLockerGateway>(...);

        return services;
    }
}
