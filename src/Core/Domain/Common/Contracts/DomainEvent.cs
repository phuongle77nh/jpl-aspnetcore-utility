using JPL.NetCoreUtility.Shared.Events;

namespace JPL.NetCoreUtility.Domain.Common.Contracts;

public abstract class DomainEvent : IEvent
{
    public DateTime TriggeredOn { get; protected set; } = DateTime.UtcNow;
}