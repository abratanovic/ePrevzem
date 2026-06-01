using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Lockers;

public sealed class StationClaimRepository : IStationClaimRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public StationClaimRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StationClaim?> GetActiveClaimForStationAsync(
        PickupStationId stationId,
        CancellationToken cancellationToken = default)
        => _dbContext.StationClaims
            .SingleOrDefaultAsync(
                x => x.PickupStationId == stationId && x.ReleasedAt == null,
                cancellationToken);

    public Task<StationClaim?> GetActiveClaimByIdForOrganizationAsync(
        StationClaimId id,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => _dbContext.StationClaims
            .SingleOrDefaultAsync(
                x => x.Id == id
                    && x.OrganizationId == organizationId
                    && x.ReleasedAt == null,
                cancellationToken);

    public async Task<IReadOnlyList<StationClaim>> GetActiveClaimsForOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => await _dbContext.StationClaims
            .Where(x => x.OrganizationId == organizationId && x.ReleasedAt == null)
            .ToListAsync(cancellationToken);

    public Task AddAsync(StationClaim claim, CancellationToken cancellationToken = default)
        => _dbContext.StationClaims.AddAsync(claim, cancellationToken).AsTask();
}
