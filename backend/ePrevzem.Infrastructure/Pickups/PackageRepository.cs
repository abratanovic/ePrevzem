using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Pickups;

public sealed class PackageRepository : IPackageRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public PackageRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByReferenceAsync(string reference, CancellationToken cancellationToken = default)
        => _dbContext.Packages.AnyAsync(x => x.Reference == reference, cancellationToken);

    public Task<Package?> GetByIdAsync(PackageId id, CancellationToken cancellationToken = default)
        => _dbContext.Packages
            .Include(x => x.Placements)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Package?> GetByIdForOrganizationAsync(
        PackageId id,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => _dbContext.Packages
            .Include(x => x.Placements)
            .SingleOrDefaultAsync(
            x => x.Id == id && x.OrganizationId == organizationId,
            cancellationToken);

    public Task AddAsync(Package package, CancellationToken cancellationToken = default)
        => _dbContext.Packages.AddAsync(package, cancellationToken).AsTask();

    public void Remove(Package package)
        => _dbContext.Packages.Remove(package);
}
