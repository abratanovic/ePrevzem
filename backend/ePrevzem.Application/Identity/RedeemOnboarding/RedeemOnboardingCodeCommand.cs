using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentValidation;
using MediatR;

namespace ePrevzem.Application.Identity.RedeemOnboarding;

public sealed record RedeemOnboardingCodeCommand(
    string Code,
    string PublicKeyPem,
    string DeviceFingerprint,
    string? Label) : IRequest<DeviceSessionResponse>;

public sealed class RedeemOnboardingCodeCommandHandler
    : IRequestHandler<RedeemOnboardingCodeCommand, DeviceSessionResponse>
{
    private readonly ICitizenActivationCodeRepository _citizenCodeRepo;
    private readonly IProvisioningCodeRepository _provisioningCodeRepo;
    private readonly ICitizenUserRepository _citizenUserRepo;
    private readonly IEmployeeAccountRepository _employeeRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;

    public RedeemOnboardingCodeCommandHandler(
        ICitizenActivationCodeRepository citizenCodeRepo,
        IProvisioningCodeRepository provisioningCodeRepo,
        ICitizenUserRepository citizenUserRepo,
        IEmployeeAccountRepository employeeRepo,
        IOrganizationRepository orgRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork uow,
        IClock clock,
        ITokenService tokenService)
    {
        _citizenCodeRepo = citizenCodeRepo;
        _provisioningCodeRepo = provisioningCodeRepo;
        _citizenUserRepo = citizenUserRepo;
        _employeeRepo = employeeRepo;
        _orgRepo = orgRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _uow = uow;
        _clock = clock;
        _tokenService = tokenService;
    }

    public async Task<DeviceSessionResponse> Handle(
        RedeemOnboardingCodeCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(command.PublicKeyPem);

        // Try citizen code first
        var citizenCode = await _citizenCodeRepo.GetByCodeAsync(command.Code, cancellationToken);
        if (citizenCode is not null)
        {
            if (!citizenCode.IsRedeemable(now))
                throw new OnboardingCodeExpiredException();

            var citizen = await _citizenUserRepo.GetByIdAsync(citizenCode.CitizenUserId, cancellationToken)
                ?? throw new InvalidOperationException("Citizen user not found.");

            var device = citizen.RegisterDevice(
                CitizenDeviceId.New(),
                publicKeyBytes,
                command.DeviceFingerprint,
                command.Label,
                now);

            citizenCode.Redeem(now);
            await _citizenUserRepo.AddAsync(citizen, cancellationToken);
            await _citizenCodeRepo.AddAsync(citizenCode, cancellationToken);

            var response = await DeviceSessionFactory.ForCitizenAsync(
                citizen,
                device.Id.Value,
                _tokenService,
                _refreshTokenRepo,
                now,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return response;
        }

        // Try provisioning code
        var provisioningCode = await _provisioningCodeRepo.GetByCodeAsync(command.Code, cancellationToken);
        if (provisioningCode is not null)
        {
            if (!provisioningCode.IsRedeemable(now))
                throw new OnboardingCodeExpiredException();

            EmployeeAccount employee;

            // Resolve or create employee account
            if (provisioningCode.IsReprovisioningOfEmployeeAccountId is not null)
            {
                employee = await _employeeRepo.GetByIdAsync(provisioningCode.IsReprovisioningOfEmployeeAccountId.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Employee account not found.");
            }
            else
            {
                employee = EmployeeAccount.Create(
                    EmployeeAccountId.New(),
                    provisioningCode.OrganizationId,
                    provisioningCode.PreFilledInfo.FirstName,
                    provisioningCode.PreFilledInfo.LastName,
                    provisioningCode.PreFilledInfo.Email,
                    provisioningCode.Roles.ToList(),
                    new List<PickupStationId>(),
                    provisioningCode.Id,
                    now);
            }

            var device = employee.RegisterDevice(
                EmployeeDeviceId.New(),
                publicKeyBytes,
                command.DeviceFingerprint,
                command.Label,
                now);

            provisioningCode.Redeem(now, employee.Id);
            await _provisioningCodeRepo.AddAsync(provisioningCode, cancellationToken);

            if (provisioningCode.IsReprovisioningOfEmployeeAccountId is null)
            {
                await _employeeRepo.AddAsync(employee, cancellationToken);
            }

            var org = await _orgRepo.GetByIdAsync(provisioningCode.OrganizationId, cancellationToken);
            var response = await DeviceSessionFactory.ForEmployeeAsync(
                employee,
                device.Id.Value,
                org?.Name,
                _tokenService,
                _refreshTokenRepo,
                now,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return response;
        }

        throw new OnboardingCodeNotFoundException();
    }
}

public sealed class RedeemOnboardingCodeCommandValidator : AbstractValidator<RedeemOnboardingCodeCommand>
{
    public RedeemOnboardingCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.");

        RuleFor(x => x.PublicKeyPem)
            .NotEmpty().WithMessage("Public key is required.")
            .Must(k => k.Contains("BEGIN PUBLIC KEY")).WithMessage("Invalid public key format.");

        RuleFor(x => x.DeviceFingerprint)
            .NotEmpty().WithMessage("Device fingerprint is required.");
    }
}
