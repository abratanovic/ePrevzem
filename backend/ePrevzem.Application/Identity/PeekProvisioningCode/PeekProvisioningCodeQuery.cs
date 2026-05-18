using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.PeekProvisioningCode;

public sealed record PeekProvisioningCodeQuery(string Code) : IRequest<PeekProvisioningCodeResult>;

public sealed record PeekProvisioningCodeResult(
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<string> Roles,
    Guid OrganizationId,
    string OrganizationName,
    DateTimeOffset ExpiresAt);

public sealed class PeekProvisioningCodeQueryHandler
    : IRequestHandler<PeekProvisioningCodeQuery, PeekProvisioningCodeResult>
{
    private readonly IProvisioningCodeRepository _provisioningCodeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IClock _clock;

    public PeekProvisioningCodeQueryHandler(
        IProvisioningCodeRepository provisioningCodeRepository,
        IOrganizationRepository organizationRepository,
        IClock clock)
    {
        _provisioningCodeRepository = provisioningCodeRepository;
        _organizationRepository = organizationRepository;
        _clock = clock;
    }

    public async Task<PeekProvisioningCodeResult> Handle(
        PeekProvisioningCodeQuery query,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var code = await _provisioningCodeRepository.GetByCodeAsync(query.Code, cancellationToken);
        if (code is null)
            throw new ProvisioningCodeNotFoundException(query.Code);

        if (code.RedeemedAt is not null)
            throw new ProvisioningCodeAlreadyRedeemedException(query.Code);

        if (!code.IsRedeemable(now))
            throw new ProvisioningCodeExpiredException(query.Code);

        var org = await _organizationRepository.GetByIdAsync(code.OrganizationId, cancellationToken);

        return new PeekProvisioningCodeResult(
            code.PreFilledFirstName,
            code.PreFilledLastName,
            code.PreFilledEmail,
            code.Roles.Select(r => r.ToString()).ToList(),
            code.OrganizationId.Value,
            org?.Name ?? string.Empty,
            code.ExpiresAt);
    }
}

public sealed class ProvisioningCodeNotFoundException(string code)
    : Exception($"Provisioning code '{code}' was not found.");

public sealed class ProvisioningCodeExpiredException(string code)
    : Exception($"Provisioning code '{code}' has expired.");

public sealed class ProvisioningCodeAlreadyRedeemedException(string code)
    : Exception($"Provisioning code '{code}' has already been redeemed.");
