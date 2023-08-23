using JPL.NetCoreUtility.Shared.Events;

namespace JPL.NetCoreUtility.Application.Common.Events;

public interface IEventPublisher : ITransientService
{
    Task PublishAsync(IEvent @event);
}