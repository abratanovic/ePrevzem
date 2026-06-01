using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using MediatR;

namespace ePrevzem.Application.Pickups.LookupRecipient;

public sealed record LookupPickupRecipientQuery(string Emso) : IRequest<PickupRecipientResponse>;

public sealed class LookupPickupRecipientQueryHandler
    : IRequestHandler<LookupPickupRecipientQuery, PickupRecipientResponse>
{
    private readonly ICitizenUserRepository _citizenRepository;

    public LookupPickupRecipientQueryHandler(ICitizenUserRepository citizenRepository)
    {
        _citizenRepository = citizenRepository;
    }

    public async Task<PickupRecipientResponse> Handle(
        LookupPickupRecipientQuery query,
        CancellationToken cancellationToken)
    {
        var citizen = await _citizenRepository.GetByEmsoAsync(query.Emso.Trim(), cancellationToken)
            ?? throw new PickupRecipientNotFoundException();

        return new PickupRecipientResponse(citizen.Id.Value, citizen.FirstName, citizen.LastName);
    }
}

public sealed class PickupRecipientNotFoundException()
    : Exception("Pickup recipient was not found.");
