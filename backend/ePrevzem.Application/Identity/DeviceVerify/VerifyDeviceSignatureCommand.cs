using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Domain.Identity;
using FluentValidation;
using MediatR;

namespace ePrevzem.Application.Identity.DeviceVerify;

public sealed record VerifyDeviceSignatureCommand(Guid DeviceId, string SignatureBase64) : IRequest<DeviceSessionResponse>;

public sealed class VerifyDeviceSignatureCommandHandler
    : IRequestHandler<VerifyDeviceSignatureCommand, DeviceSessionResponse>
{
    private readonly IDeviceChallengeRepository _challengeRepo;
    private readonly ICitizenUserRepository _citizenUserRepo;
    private readonly IEmployeeAccountRepository _employeeRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ISignatureVerifier _verifier;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;

    public VerifyDeviceSignatureCommandHandler(
        IDeviceChallengeRepository challengeRepo,
        ICitizenUserRepository citizenUserRepo,
        IEmployeeAccountRepository employeeRepo,
        IOrganizationRepository orgRepo,
        IRefreshTokenRepository refreshTokenRepo,
        ISignatureVerifier verifier,
        IUnitOfWork uow,
        IClock clock,
        ITokenService tokenService)
    {
        _challengeRepo = challengeRepo;
        _citizenUserRepo = citizenUserRepo;
        _employeeRepo = employeeRepo;
        _orgRepo = orgRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _verifier = verifier;
        _uow = uow;
        _clock = clock;
        _tokenService = tokenService;
    }

    public async Task<DeviceSessionResponse> Handle(
        VerifyDeviceSignatureCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var deviceId = new CitizenDeviceId(command.DeviceId);

        var challenge = await _challengeRepo.GetLatestActiveAsync(command.DeviceId, now, cancellationToken)
            ?? throw new InvalidChallengeException();

        // Resolve owner: try citizen first
        var citizen = await _citizenUserRepo.GetByCitizenDeviceIdAsync(deviceId, cancellationToken);
        if (citizen is not null)
        {
            var device = citizen.Devices.FirstOrDefault(d => d.Id == deviceId)
                ?? throw new InvalidChallengeException();

            var publicKeyPem = System.Text.Encoding.UTF8.GetString(device.PublicKey);
            try
            {
                _verifier.Verify(publicKeyPem, challenge.Nonce, Convert.FromBase64String(command.SignatureBase64));
            }
            catch (Exception)
            {
                throw new InvalidSignatureException();
            }

            challenge.Consume(now);
            await _challengeRepo.AddAsync(challenge, cancellationToken);

            var response = await DeviceSessionFactory.ForCitizenAsync(
                citizen,
                command.DeviceId,
                _tokenService,
                _refreshTokenRepo,
                now,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return response;
        }

        // Try employee
        var employeeDeviceId = new EmployeeDeviceId(command.DeviceId);
        var employee = await _employeeRepo.GetByEmployeeDeviceIdAsync(employeeDeviceId, cancellationToken);
        if (employee is not null)
        {
            var device = employee.Devices.FirstOrDefault(d => d.Id == employeeDeviceId)
                ?? throw new InvalidChallengeException();

            var publicKeyPem = System.Text.Encoding.UTF8.GetString(device.PublicKey);
            try
            {
                _verifier.Verify(publicKeyPem, challenge.Nonce, Convert.FromBase64String(command.SignatureBase64));
            }
            catch (Exception)
            {
                throw new InvalidSignatureException();
            }

            challenge.Consume(now);
            await _challengeRepo.AddAsync(challenge, cancellationToken);

            var org = await _orgRepo.GetByIdAsync(employee.OrganizationId, cancellationToken);
            var response = await DeviceSessionFactory.ForEmployeeAsync(
                employee,
                command.DeviceId,
                org?.Name,
                _tokenService,
                _refreshTokenRepo,
                now,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return response;
        }

        throw new DeviceNotFoundException();
    }
}

public sealed class VerifyDeviceSignatureCommandValidator : AbstractValidator<VerifyDeviceSignatureCommand>
{
    public VerifyDeviceSignatureCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("Device ID is required.");

        RuleFor(x => x.SignatureBase64)
            .NotEmpty().WithMessage("Signature is required.");
    }
}
