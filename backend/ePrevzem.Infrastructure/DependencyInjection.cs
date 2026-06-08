using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Infrastructure.Audit;
using ePrevzem.Infrastructure.Identity;
using ePrevzem.Infrastructure.Lockers;
using ePrevzem.Infrastructure.Organizations;
using ePrevzem.Infrastructure.Persistence;
using ePrevzem.Infrastructure.Pickups;
using ePrevzem.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationAdminAccountRepository, OrganizationAdminAccountRepository>();
        services.AddScoped<IEmployeeAccountRepository, EmployeeAccountRepository>();
        services.AddScoped<IProvisioningCodeRepository, ProvisioningCodeRepository>();
        services.AddScoped<ICitizenUserRepository, CitizenUserRepository>();
        services.AddScoped<ICitizenActivationCodeRepository, CitizenActivationCodeRepository>();
        services.AddScoped<IPickupStationRepository, PickupStationRepository>();
        services.AddScoped<IStationClaimRepository, StationClaimRepository>();
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IPickupReadRepository, PickupReadRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditContextLookup, AuditContextLookup>();
        services.AddScoped<IDeviceChallengeRepository, DeviceChallengeRepository>();

        var siTrustSecret = configuration["SiTrust:Secret"]
            ?? throw new InvalidOperationException("SiTrust:Secret is not configured");
        services.AddSingleton<ISiTrustTokenValidator>(
            new SiTrustTokenValidator(new SiTrustOptions { Secret = siTrustSecret }));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPackageReferenceGenerator, PackageReferenceGenerator>();
        services.AddSingleton<ISignatureVerifier, EcdsaSignatureVerifier>();
        services.AddHostedService<IdentitySeeder>();

        // Outbound ports.
        services.Configure<Direct4MeOptions>(configuration.GetSection("Direct4Me"));
        services.AddHttpClient<ILockerGateway, Direct4MeLockerGateway>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<Direct4MeOptions>>().Value;
            var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
            client.BaseAddress = new Uri(baseUrl);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
        });

        services.Configure<CvIdentityOptions>(configuration.GetSection("CvIdentity"));
        services.AddHttpClient<ICvIdentityClient, CvIdentityClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CvIdentityOptions>>().Value;
            var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
