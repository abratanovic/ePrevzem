namespace ePrevzem.Application.Common.Abstractions;

public interface IEPrevzemDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
