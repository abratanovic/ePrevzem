using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface ICitizenActivationCodeRepository
{
    Task<CitizenActivationCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(CitizenActivationCode code, CancellationToken cancellationToken = default);
}
