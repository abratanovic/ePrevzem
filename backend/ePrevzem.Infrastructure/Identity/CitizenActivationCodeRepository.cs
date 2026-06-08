using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Infrastructure.Identity;

public sealed class CitizenActivationCodeRepository : ICitizenActivationCodeRepository
{
    private readonly EPrevzemDbContext _dbContext;

    public CitizenActivationCodeRepository(EPrevzemDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CitizenActivationCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _dbContext.CitizenActivationCodes.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);

    public Task AddAsync(CitizenActivationCode code, CancellationToken cancellationToken = default)
        => _dbContext.CitizenActivationCodes.AddAsync(code, cancellationToken).AsTask();
}
