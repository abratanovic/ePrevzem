using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Pickups.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;
using MediatR;

namespace ePrevzem.Application.Pickups.Queries;

public sealed record GetCitizenPickupsQuery(Guid CitizenUserId)
    : IRequest<IReadOnlyList<CitizenPickupResponse>>;

public sealed record GetCitizenPickupDetailQuery(Guid CitizenUserId, Guid PickupId)
    : IRequest<CitizenPickupDetailResponse?>;

public sealed class GetCitizenPickupsQueryHandler
    : IRequestHandler<GetCitizenPickupsQuery, IReadOnlyList<CitizenPickupResponse>>
{
    private readonly IPickupReadRepository _repository;
    private readonly IClock _clock;

    public GetCitizenPickupsQueryHandler(IPickupReadRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public Task<IReadOnlyList<CitizenPickupResponse>> Handle(
        GetCitizenPickupsQuery query,
        CancellationToken cancellationToken)
        => _repository.GetForCitizenAsync(
            new CitizenUserId(query.CitizenUserId),
            _clock.UtcNow,
            cancellationToken);
}

public sealed class GetCitizenPickupDetailQueryHandler
    : IRequestHandler<GetCitizenPickupDetailQuery, CitizenPickupDetailResponse?>
{
    private readonly IPickupReadRepository _repository;
    private readonly IClock _clock;

    public GetCitizenPickupDetailQueryHandler(IPickupReadRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public Task<CitizenPickupDetailResponse?> Handle(
        GetCitizenPickupDetailQuery query,
        CancellationToken cancellationToken)
        => _repository.GetCitizenPickupDetailAsync(
            new CitizenUserId(query.CitizenUserId),
            new PackageId(query.PickupId),
            _clock.UtcNow,
            cancellationToken);
}
