using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Pickups.Queries;

public sealed record GetRecentPickupsQuery(Guid OrganizationId, int Limit) : IRequest<IReadOnlyList<PickupResponse>>;
public sealed record GetAllPickupsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<PickupResponse>>;
public sealed record GetPickupStationOptionsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<PickupStationOptionResponse>>;
public sealed record GetDashboardStatsQuery(Guid OrganizationId) : IRequest<DashboardStatsResponse>;
public sealed record GetLockerOccupancyQuery(Guid OrganizationId) : IRequest<IReadOnlyList<LockerOccupancyResponse>>;

public sealed class GetRecentPickupsQueryHandler
    : IRequestHandler<GetRecentPickupsQuery, IReadOnlyList<PickupResponse>>
{
    private readonly IPickupReadRepository _repository;
    public GetRecentPickupsQueryHandler(IPickupReadRepository repository) => _repository = repository;

    public Task<IReadOnlyList<PickupResponse>> Handle(GetRecentPickupsQuery query, CancellationToken cancellationToken)
        => _repository.GetRecentAsync(new OrganizationId(query.OrganizationId), Math.Clamp(query.Limit, 1, 100), cancellationToken);
}

public sealed class GetAllPickupsQueryHandler
    : IRequestHandler<GetAllPickupsQuery, IReadOnlyList<PickupResponse>>
{
    private readonly IPickupReadRepository _repository;
    public GetAllPickupsQueryHandler(IPickupReadRepository repository) => _repository = repository;

    public Task<IReadOnlyList<PickupResponse>> Handle(GetAllPickupsQuery query, CancellationToken cancellationToken)
        => _repository.GetAllAsync(new OrganizationId(query.OrganizationId), cancellationToken);
}

public sealed class GetPickupStationOptionsQueryHandler
    : IRequestHandler<GetPickupStationOptionsQuery, IReadOnlyList<PickupStationOptionResponse>>
{
    private readonly IPickupReadRepository _repository;
    public GetPickupStationOptionsQueryHandler(IPickupReadRepository repository) => _repository = repository;

    public Task<IReadOnlyList<PickupStationOptionResponse>> Handle(GetPickupStationOptionsQuery query, CancellationToken cancellationToken)
        => _repository.GetStationOptionsAsync(new OrganizationId(query.OrganizationId), cancellationToken);
}

public sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    private readonly IPickupReadRepository _repository;
    private readonly IClock _clock;
    public GetDashboardStatsQueryHandler(IPickupReadRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public Task<DashboardStatsResponse> Handle(GetDashboardStatsQuery query, CancellationToken cancellationToken)
        => _repository.GetDashboardStatsAsync(new OrganizationId(query.OrganizationId), _clock.UtcNow, cancellationToken);
}

public sealed class GetLockerOccupancyQueryHandler
    : IRequestHandler<GetLockerOccupancyQuery, IReadOnlyList<LockerOccupancyResponse>>
{
    private readonly IPickupReadRepository _repository;
    public GetLockerOccupancyQueryHandler(IPickupReadRepository repository) => _repository = repository;

    public Task<IReadOnlyList<LockerOccupancyResponse>> Handle(GetLockerOccupancyQuery query, CancellationToken cancellationToken)
        => _repository.GetLockerOccupancyAsync(new OrganizationId(query.OrganizationId), cancellationToken);
}
