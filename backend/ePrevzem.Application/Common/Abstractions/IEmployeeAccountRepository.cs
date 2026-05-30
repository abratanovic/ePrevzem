using ePrevzem.Domain.Identity;

namespace ePrevzem.Application.Common.Abstractions;

public interface IEmployeeAccountRepository
{
    Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default);
}
