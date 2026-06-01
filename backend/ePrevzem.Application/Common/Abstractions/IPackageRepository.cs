using ePrevzem.Domain.Pickups;

namespace ePrevzem.Application.Common.Abstractions;

public interface IPackageRepository
{
    Task<bool> ExistsByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task AddAsync(Package package, CancellationToken cancellationToken = default);
}
