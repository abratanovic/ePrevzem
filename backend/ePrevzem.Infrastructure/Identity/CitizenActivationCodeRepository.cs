using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;

namespace ePrevzem.Infrastructure.Identity;

public sealed class CitizenActivationCodeRepository : ICitizenActivationCodeRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public CitizenActivationCodeRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(CitizenActivationCode code, CancellationToken cancellationToken = default)
        => _dbContext.CitizenActivationCodes.AddAsync(code, cancellationToken).AsTask();
}
