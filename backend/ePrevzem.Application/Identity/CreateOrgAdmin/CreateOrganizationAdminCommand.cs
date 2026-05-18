using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Identity.CreateOrgAdmin;

public sealed record CreateOrganizationAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    Guid OrganizationId) : IRequest<CreateOrganizationAdminResponse>;

public sealed class CreateOrganizationAdminCommandHandler
    : IRequestHandler<CreateOrganizationAdminCommand, CreateOrganizationAdminResponse>
{
    private readonly IOrganizationAdminAccountRepository _orgAdminRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateOrganizationAdminCommandHandler(
        IOrganizationAdminAccountRepository orgAdminRepository,
        IOrganizationRepository organizationRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _orgAdminRepository = orgAdminRepository;
        _organizationRepository = organizationRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CreateOrganizationAdminResponse> Handle(
        CreateOrganizationAdminCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var orgId = new OrganizationId(command.OrganizationId);
        var org = await _organizationRepository.GetByIdAsync(orgId, cancellationToken)
            ?? throw new OrganizationNotFoundException(command.OrganizationId);

        if (await _orgAdminRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            throw new DuplicateOrgAdminEmailException(normalizedEmail);

        var temporaryPassword = GenerateTemporaryPassword();
        var passwordHash = _passwordHasher.Hash(temporaryPassword);

        var account = OrganizationAdminAccount.Create(
            OrganizationAdminAccountId.New(),
            org.Id,
            command.FirstName,
            command.LastName,
            normalizedEmail,
            passwordHash,
            now);

        await _orgAdminRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrganizationAdminResponse(
            account.Id.Value,
            account.Email,
            account.OrganizationId.Value,
            temporaryPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%";
        const string all = upper + lower + digits + symbols;

        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < 12; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}

public sealed class OrganizationNotFoundException(Guid organizationId)
    : Exception($"Organization '{organizationId}' was not found.");

public sealed class DuplicateOrgAdminEmailException(string email)
    : Exception($"An organization admin with email '{email}' already exists.");
