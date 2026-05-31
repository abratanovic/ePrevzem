using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface ICitizenActivationCodeRepository
{
    Task AddAsync(CitizenActivationCode code, CancellationToken cancellationToken = default);
}
