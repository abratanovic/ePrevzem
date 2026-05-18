using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Lockers;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Lockers;

public sealed class PickupStationRepository : IPickupStationRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public PickupStationRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PickupStation?> GetByIdAsync(PickupStationId id, CancellationToken cancellationToken = default)
        => _dbContext.PickupStations
            .Include(x => x.Lockers)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<PickupStation?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        => _dbContext.PickupStations
            .Include(x => x.Lockers)
            .SingleOrDefaultAsync(x => x.SerialNumber == serialNumber, cancellationToken);

    public Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        => _dbContext.PickupStations.AnyAsync(x => x.SerialNumber == serialNumber, cancellationToken);

    public Task AddAsync(PickupStation station, CancellationToken cancellationToken = default)
        => _dbContext.PickupStations.AddAsync(station, cancellationToken).AsTask();
}
