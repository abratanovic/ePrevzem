using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.PeekOnboarding;

public sealed record PeekOnboardingCodeQuery(string Code) : IRequest<OnboardingPreview>;

public sealed record OnboardingPreview(
    string Kind,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? Emso,
    string? OrganizationName,
    IReadOnlyList<string> Roles,
    DateTimeOffset ExpiresAt);

public sealed class PeekOnboardingCodeQueryHandler
    : IRequestHandler<PeekOnboardingCodeQuery, OnboardingPreview>
{
    private readonly ICitizenActivationCodeRepository _citizenActivationCodeRepository;
    private readonly ICitizenUserRepository _citizenUserRepository;
    private readonly IProvisioningCodeRepository _provisioningCodeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IClock _clock;

    public PeekOnboardingCodeQueryHandler(
        ICitizenActivationCodeRepository citizenActivationCodeRepository,
        ICitizenUserRepository citizenUserRepository,
        IProvisioningCodeRepository provisioningCodeRepository,
        IOrganizationRepository organizationRepository,
        IClock clock)
    {
        _citizenActivationCodeRepository = citizenActivationCodeRepository;
        _citizenUserRepository = citizenUserRepository;
        _provisioningCodeRepository = provisioningCodeRepository;
        _organizationRepository = organizationRepository;
        _clock = clock;
    }

    public async Task<OnboardingPreview> Handle(
        PeekOnboardingCodeQuery query,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        // Try citizen activation code first
        var citizenCode = await _citizenActivationCodeRepository.GetByCodeAsync(query.Code, cancellationToken);
        if (citizenCode is not null)
        {
            if (!citizenCode.IsRedeemable(now))
                throw new OnboardingCodeExpiredException();

            var citizenUser = await _citizenUserRepository.GetByIdAsync(citizenCode.CitizenUserId, cancellationToken);
            if (citizenUser is null)
                throw new OnboardingCodeNotFoundException();

            return new OnboardingPreview(
                Kind: "Citizen",
                FirstName: citizenUser.FirstName,
                LastName: citizenUser.LastName,
                Email: citizenUser.Email,
                PhoneNumber: citizenUser.PhoneNumber,
                Emso: citizenUser.Emso,
                OrganizationName: null,
                Roles: new List<string>(),
                ExpiresAt: citizenCode.ExpiresAt);
        }

        // Try provisioning code
        var provisioningCode = await _provisioningCodeRepository.GetByCodeAsync(query.Code, cancellationToken);
        if (provisioningCode is not null)
        {
            if (!provisioningCode.IsRedeemable(now))
                throw new OnboardingCodeExpiredException();

            var org = await _organizationRepository.GetByIdAsync(provisioningCode.OrganizationId, cancellationToken);
            var organizationName = org?.Name ?? string.Empty;

            return new OnboardingPreview(
                Kind: "Employee",
                FirstName: provisioningCode.PreFilledInfo.FirstName,
                LastName: provisioningCode.PreFilledInfo.LastName,
                Email: provisioningCode.PreFilledInfo.Email,
                PhoneNumber: null,
                Emso: null,
                OrganizationName: organizationName,
                Roles: provisioningCode.Roles.Select(r => r.ToString()).ToList(),
                ExpiresAt: provisioningCode.ExpiresAt);
        }

        throw new OnboardingCodeNotFoundException();
    }
}
