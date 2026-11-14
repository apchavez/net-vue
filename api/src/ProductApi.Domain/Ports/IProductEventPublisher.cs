using ProductApi.Domain.Events;

namespace ProductApi.Domain.Ports;

public interface IProductEventPublisher
{
    Task PublishAsync(ProductEvent @event, CancellationToken ct = default);
}
