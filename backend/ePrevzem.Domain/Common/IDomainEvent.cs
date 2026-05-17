namespace ePrevzem.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
