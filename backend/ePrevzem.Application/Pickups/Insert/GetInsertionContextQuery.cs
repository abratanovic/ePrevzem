using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Pickups.Insert;

/// <summary>
/// Resolves the insertion context for an Operator who scanned a station serial:
/// the station, the packages awaiting placement there, and the free lockers.
/// </summary>
public sealed record GetInsertionContextQuery(Guid OrganizationId, string SerialNumber)
    : IRequest<InsertionContextResponse>;

public sealed class GetInsertionContextQueryHandler
    : IRequestHandler<GetInsertionContextQuery, InsertionContextResponse>
{
    private readonly IPickupReadRepository _readRepository;

    public GetInsertionContextQueryHandler(IPickupReadRepository readRepository)
        => _readRepository = readRepository;

    public async Task<InsertionContextResponse> Handle(
        GetInsertionContextQuery query,
        CancellationToken cancellationToken)
    {
        var context = await _readRepository.GetInsertionContextAsync(
            new OrganizationId(query.OrganizationId),
            query.SerialNumber.Trim(),
            cancellationToken);

        return context ?? throw new InsertionStationNotFoundException();
    }
}
