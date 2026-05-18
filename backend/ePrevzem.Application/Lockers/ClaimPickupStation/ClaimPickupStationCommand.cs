using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Lockers.Dtos;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Lockers.ClaimPickupStation;

public sealed record ClaimPickupStationCommand(
    Guid OrganizationId,
    string SerialNumber,
    decimal Latitude,
    decimal Longitude,
    string Address,
    string HouseNumber,
    string ZipCode,
    string City) : IRequest<StationClaimResponse>;

public sealed class ClaimPickupStationCommandHandler
    : IRequestHandler<ClaimPickupStationCommand, StationClaimResponse>
{
    private readonly IPickupStationRepository _stationRepository;
    private readonly IStationClaimRepository _claimRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClaimPickupStationCommandHandler(
        IPickupStationRepository stationRepository,
        IStationClaimRepository claimRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _stationRepository = stationRepository;
        _claimRepository = claimRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<StationClaimResponse> Handle(
        ClaimPickupStationCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepository.GetBySerialNumberAsync(command.SerialNumber, cancellationToken)
            ?? throw new PickupStationNotFoundException(command.SerialNumber);

        var existing = await _claimRepository.GetActiveClaimForStationAsync(station.Id, cancellationToken);
        if (existing is not null)
            throw new StationAlreadyClaimedException(command.SerialNumber);

        var location = Location.Create(
            command.Latitude,
            command.Longitude,
            command.Address,
            command.HouseNumber,
            command.ZipCode,
            command.City);

        var claim = StationClaim.Claim(
            StationClaimId.New(),
            station.Id,
            new OrganizationId(command.OrganizationId),
            location,
            _clock.UtcNow);

        await _claimRepository.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StationClaimResponse(
            claim.Id.Value,
            claim.PickupStationId.Value,
            claim.OrganizationId.Value,
            new LocationResponse(
                claim.Location.Latitude,
                claim.Location.Longitude,
                claim.Location.Address,
                claim.Location.HouseNumber,
                claim.Location.ZipCode,
                claim.Location.City),
            claim.ClaimedAt);
    }
}

public sealed class PickupStationNotFoundException(string serialNumber)
    : Exception($"Pickup station with serial number '{serialNumber}' was not found.");

public sealed class StationAlreadyClaimedException(string serialNumber)
    : Exception($"Pickup station with serial number '{serialNumber}' is already claimed by another organization.");
