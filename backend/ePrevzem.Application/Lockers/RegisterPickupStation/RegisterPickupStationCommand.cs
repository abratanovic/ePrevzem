using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Domain.Lockers;
using MediatR;

namespace ePrevzem.Application.Lockers.RegisterPickupStation;

public sealed record RegisterPickupStationCommand(
    string SerialNumber,
    IReadOnlyList<int> LockerNumbers) : IRequest<PickupStationResponse>;

public sealed class RegisterPickupStationCommandHandler
    : IRequestHandler<RegisterPickupStationCommand, PickupStationResponse>
{
    private readonly IPickupStationRepository _stationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RegisterPickupStationCommandHandler(
        IPickupStationRepository stationRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _stationRepository = stationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<PickupStationResponse> Handle(
        RegisterPickupStationCommand command,
        CancellationToken cancellationToken)
    {
        if (await _stationRepository.ExistsBySerialNumberAsync(command.SerialNumber, cancellationToken))
            throw new DuplicateSerialNumberException(command.SerialNumber);

        var station = PickupStation.Create(PickupStationId.New(), command.SerialNumber, _clock.UtcNow);

        foreach (var number in command.LockerNumbers)
            station.AddLocker(LockerId.New(), number);

        await _stationRepository.AddAsync(station, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PickupStationResponse(
            station.Id.Value,
            station.SerialNumber,
            station.Lockers.Select(l => new LockerResponse(l.Id.Value, l.LockerNumber)).ToList(),
            station.CreatedAt);
    }
}

public sealed class DuplicateSerialNumberException(string serialNumber)
    : Exception($"A pickup station with serial number '{serialNumber}' already exists.");
