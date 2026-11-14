using ProductApi.Domain;

namespace ProductApi.Domain.Events;

public enum ProductEventType
{
    ProductCreated,
    ProductUpdated,
    ProductDeleted
}

public sealed record ProductEvent(string EventId, ProductEventType EventType, string OccurredAt, Product Product)
{
    public static ProductEvent Of(ProductEventType type, Product product) =>
        new(Guid.NewGuid().ToString(), type, DateTimeOffset.UtcNow.ToString("O"), product);
}
