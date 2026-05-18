using ePrevzem.Domain.Lockers;

namespace ePrevzem.Application.Common.Abstractions;

public interface IPickupStationRepository
{
    Task<PickupStation?> GetByIdAsync(PickupStationId id, CancellationToken cancellationToken = default);
    Task<PickupStation?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task AddAsync(PickupStation station, CancellationToken cancellationToken = default);
}
