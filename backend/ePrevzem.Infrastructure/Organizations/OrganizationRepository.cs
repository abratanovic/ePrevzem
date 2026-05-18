using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Organizations;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Organizations;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public OrganizationRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default)
        => _dbContext.Organizations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default)
        => _dbContext.Organizations.AnyAsync(x => x.TaxNumber == taxNumber, cancellationToken);

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => _dbContext.Organizations.AnyAsync(x => x.RegistrationNumber == registrationNumber, cancellationToken);

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
        => _dbContext.Organizations.AddAsync(organization, cancellationToken).AsTask();
}
