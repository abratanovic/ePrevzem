using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Delete;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Open;

/// <summary>
/// Citizen opens the locker holding their package. Resolves the box server-side
/// from the package's active placement and asks the locker gateway to open it.
/// Does NOT change package state — pickup is committed separately on confirm.
/// </summary>
public sealed record OpenCitizenPickupCommand(Guid CitizenUserId, Guid PickupId)
    : IRequest<LockerTokenResponse>;

public sealed class OpenCitizenPickupCommandHandler
    : IRequestHandler<OpenCitizenPickupCommand, LockerTokenResponse>
{
    private readonly IPickupReadRepository _readRepository;
    private readonly ILockerGateway _lockerGateway;

    public OpenCitizenPickupCommandHandler(
        IPickupReadRepository readRepository,
        ILockerGateway lockerGateway)
    {
        _readRepository = readRepository;
        _lockerGateway = lockerGateway;
    }

    public async Task<LockerTokenResponse> Handle(
        OpenCitizenPickupCommand command,
        CancellationToken cancellationToken)
    {
        var boxId = await _readRepository.GetActivePickupBoxIdAsync(
            new CitizenUserId(command.CitizenUserId),
            new PackageId(command.PickupId),
            cancellationToken)
            ?? throw new PickupNotFoundException(command.PickupId);

        var token = await _lockerGateway.OpenBoxAsync(boxId, cancellationToken);
        return new LockerTokenResponse(Convert.ToBase64String(token));
    }
}
