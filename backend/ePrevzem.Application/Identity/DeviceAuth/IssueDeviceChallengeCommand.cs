using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.DeviceAuth;

public sealed record IssueDeviceChallengeCommand(Guid DeviceId) : IRequest<DeviceChallengeResponse>;

public sealed record DeviceChallengeResponse(string Challenge, DateTimeOffset ExpiresAt);

public sealed class IssueDeviceChallengeCommandHandler
    : IRequestHandler<IssueDeviceChallengeCommand, DeviceChallengeResponse>
{
    private readonly ICitizenUserRepository _citizenUserRepository;
    private readonly IEmployeeAccountRepository _employeeAccountRepository;
    private readonly IDeviceChallengeRepository _deviceChallengeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IssueDeviceChallengeCommandHandler(
        ICitizenUserRepository citizenUserRepository,
        IEmployeeAccountRepository employeeAccountRepository,
        IDeviceChallengeRepository deviceChallengeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _citizenUserRepository = citizenUserRepository;
        _employeeAccountRepository = employeeAccountRepository;
        _deviceChallengeRepository = deviceChallengeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<DeviceChallengeResponse> Handle(
        IssueDeviceChallengeCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var deviceId = command.DeviceId;

        // Try to resolve device kind: citizen first, then employee
        var citizenUser = await _citizenUserRepository.GetByCitizenDeviceIdAsync(
            new CitizenDeviceId(deviceId), cancellationToken);

        if (citizenUser is not null)
        {
            var device = citizenUser.Devices.FirstOrDefault(d => d.Id == new CitizenDeviceId(deviceId));
            if (device is null || !device.IsActive)
                throw new DeviceNotFoundException();

            var nonce = RandomNumberGenerator.GetBytes(32);
            var challenge = DeviceChallenge.Issue(
                DeviceChallengeId.New(),
                deviceId,
                DeviceKind.Citizen,
                nonce,
                now,
                now.AddMinutes(2));

            await _deviceChallengeRepository.AddAsync(challenge, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeviceChallengeResponse(
                Challenge: Convert.ToBase64String(nonce),
                ExpiresAt: challenge.ExpiresAt);
        }

        // Try employee
        var employeeAccount = await _employeeAccountRepository.GetByEmployeeDeviceIdAsync(
            new EmployeeDeviceId(deviceId), cancellationToken);

        if (employeeAccount is not null)
        {
            var device = employeeAccount.Devices.FirstOrDefault(d => d.Id == new EmployeeDeviceId(deviceId));
            if (device is null || !device.IsActive)
                throw new DeviceNotFoundException();

            var nonce = RandomNumberGenerator.GetBytes(32);
            var challenge = DeviceChallenge.Issue(
                DeviceChallengeId.New(),
                deviceId,
                DeviceKind.Employee,
                nonce,
                now,
                now.AddMinutes(2));

            await _deviceChallengeRepository.AddAsync(challenge, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeviceChallengeResponse(
                Challenge: Convert.ToBase64String(nonce),
                ExpiresAt: challenge.ExpiresAt);
        }

        throw new DeviceNotFoundException();
    }
}
