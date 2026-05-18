using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Identity.IssueProvisioningCode;

public sealed record IssueProvisioningCodeCommand(
    Guid OrgAdminAccountId,
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<EmployeeAccountRole> Roles,
    IReadOnlyList<Guid> StationAccessIds,
    int ExpiresInHours) : IRequest<IssueProvisioningCodeResponse>;

public sealed record IssueProvisioningCodeResponse(string Code, DateTimeOffset ExpiresAt);

public sealed class IssueProvisioningCodeCommandHandler
    : IRequestHandler<IssueProvisioningCodeCommand, IssueProvisioningCodeResponse>
{
    private readonly IProvisioningCodeRepository _provisioningCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IssueProvisioningCodeCommandHandler(
        IProvisioningCodeRepository provisioningCodeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _provisioningCodeRepository = provisioningCodeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IssueProvisioningCodeResponse> Handle(
        IssueProvisioningCodeCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddHours(command.ExpiresInHours);
        var code = GenerateCode();
        var stationAccess = command.StationAccessIds.Select(id => new PickupStationId(id)).ToList();

        var provisioningCode = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            new OrganizationId(command.OrganizationId),
            code,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Roles,
            stationAccess,
            new OrganizationAdminAccountId(command.OrgAdminAccountId),
            now,
            expiresAt,
            isReprovisioningOf: null);

        await _provisioningCodeRepository.AddAsync(provisioningCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueProvisioningCodeResponse(provisioningCode.Code, provisioningCode.ExpiresAt);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new char[8];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(buffer);
    }
}
