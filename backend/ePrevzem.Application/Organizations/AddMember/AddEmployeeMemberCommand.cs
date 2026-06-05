using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.AddMember;

public sealed record AddEmployeeMemberCommand(
    Guid OrgAdminAccountId,
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string Email) : IRequest<AddEmployeeMemberResponse>;

public sealed record AddEmployeeMemberResponse(
    Guid EmployeeId,
    string InitialPassword,
    string ProvisioningCode,
    DateTimeOffset ProvisioningCodeExpiresAt);

public sealed class AddEmployeeMemberCommandHandler
    : IRequestHandler<AddEmployeeMemberCommand, AddEmployeeMemberResponse>
{
    private readonly IEmployeeAccountRepository _employeeRepo;
    private readonly IProvisioningCodeRepository _provisioningRepo;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;

    public AddEmployeeMemberCommandHandler(
        IEmployeeAccountRepository employeeRepo,
        IProvisioningCodeRepository provisioningRepo,
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IClock clock)
    {
        _employeeRepo = employeeRepo;
        _provisioningRepo = provisioningRepo;
        _uow = uow;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<AddEmployeeMemberResponse> Handle(
        AddEmployeeMemberCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var existing = await _employeeRepo.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
            throw new DuplicateEmployeeEmailException(normalizedEmail);

        var now = _clock.UtcNow;
        var orgId = new OrganizationId(command.OrganizationId);
        var adminId = new OrganizationAdminAccountId(command.OrgAdminAccountId);
        var roles = new[] { EmployeeAccountRole.RecordManager, EmployeeAccountRole.Operator };

        var provisioningCodeId = ProvisioningCodeId.New();
        var codeValue = GenerateCode();
        var expiresAt = now.AddHours(72);
        var preFilledInfo = PersonalInfo.Create(command.FirstName, command.LastName, normalizedEmail);

        var provisioningCode = ProvisioningCode.Issue(
            provisioningCodeId,
            orgId,
            codeValue,
            preFilledInfo,
            roles,
            adminId,
            now,
            expiresAt,
            isReprovisioningOf: null);

        await _provisioningRepo.AddAsync(provisioningCode, cancellationToken);

        var initialPassword = GeneratePassword();
        var passwordHash = _hasher.Hash(initialPassword);

        var employee = EmployeeAccount.Create(
            EmployeeAccountId.New(),
            orgId,
            command.FirstName,
            command.LastName,
            normalizedEmail,
            roles,
            Array.Empty<PickupStationId>(),
            provisioningCodeId,
            now);

        employee.SetInitialPassword(passwordHash, now);

        await _employeeRepo.AddAsync(employee, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new AddEmployeeMemberResponse(
            employee.Id.Value,
            initialPassword,
            provisioningCode.Code,
            provisioningCode.ExpiresAt);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new char[8];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(buffer);
    }

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjklmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all = upper + lower + digits;
        var buf = new char[14];
        buf[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        buf[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        buf[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        for (var i = 3; i < buf.Length; i++)
            buf[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        return new string(buf.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }
}

public sealed class DuplicateEmployeeEmailException(string email)
    : Exception($"An employee with email '{email}' already exists.");
