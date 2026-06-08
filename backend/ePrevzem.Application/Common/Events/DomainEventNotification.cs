using ePrevzem.Domain.Common;
using MediatR;

namespace ePrevzem.Application.Common.Events;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
