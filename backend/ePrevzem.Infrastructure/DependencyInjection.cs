using ePrevzem.Application.Common.Abstractions;
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

        services.AddDbContext<EPrevzemDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEPrevzemDbContext>(sp => sp.GetRequiredService<EPrevzemDbContext>());

        services.AddSingleton<IClock, SystemClock>();

        // Outbound ports — adapters wired here when implemented.
        // services.AddHttpClient<ISiTrustClient, SiTrustClient>(...);
        // services.AddHttpClient<ILockerGateway, Direct4MeLockerGateway>(...);

        return services;
    }
}
