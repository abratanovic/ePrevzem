using ePrevzem.Domain.Common;

namespace ePrevzem.Application.Common.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
