using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.ReissueProvisioningCode;

public sealed record ReissueProvisioningCodeCommand(
    Guid OrgAdminAccountId,
    Guid OrganizationId,
    Guid EmployeeId) : IRequest<ReissueProvisioningCodeResponse>;

public sealed record ReissueProvisioningCodeResponse(string ProvisioningCode, DateTimeOffset ExpiresAt);

public sealed class ReissueProvisioningCodeCommandHandler
    : IRequestHandler<ReissueProvisioningCodeCommand, ReissueProvisioningCodeResponse>
{
    private readonly IEmployeeAccountRepository _employeeRepo;
    private readonly IProvisioningCodeRepository _provisioningRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public ReissueProvisioningCodeCommandHandler(
        IEmployeeAccountRepository employeeRepo,
        IProvisioningCodeRepository provisioningRepo,
        IUnitOfWork uow,
        IClock clock)
    {
        _employeeRepo = employeeRepo;
        _provisioningRepo = provisioningRepo;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ReissueProvisioningCodeResponse> Handle(
        ReissueProvisioningCodeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepo.GetByIdAsync(
            new EmployeeAccountId(command.EmployeeId), cancellationToken)
            ?? throw new EmployeeNotFoundException(command.EmployeeId);

        if (employee.OrganizationId != new OrganizationId(command.OrganizationId))
            throw new EmployeeForbiddenException();

        var now = _clock.UtcNow;
        var expiresAt = now.AddHours(72);
        var codeValue = GenerateCode();
        var preFilledInfo = PersonalInfo.Create(employee.FirstName, employee.LastName, employee.Email);

        var provisioningCode = ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            new OrganizationId(command.OrganizationId),
            codeValue,
            preFilledInfo,
            employee.Roles,
            new OrganizationAdminAccountId(command.OrgAdminAccountId),
            now,
            expiresAt,
            isReprovisioningOf: employee.Id);

        await _provisioningRepo.AddAsync(provisioningCode, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ReissueProvisioningCodeResponse(provisioningCode.Code, provisioningCode.ExpiresAt);
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
