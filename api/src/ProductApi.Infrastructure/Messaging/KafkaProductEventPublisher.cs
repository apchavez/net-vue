using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using ProductApi.Domain.Events;
using ProductApi.Domain.Ports;

namespace ProductApi.Infrastructure.Messaging;

public sealed class KafkaProductEventPublisher : IProductEventPublisher, IDisposable
{
    private const string Topic = "product-events";
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProductEventPublisher> _logger;

    public KafkaProductEventPublisher(
        string bootstrapServers, ILogger<KafkaProductEventPublisher> logger,
        string? saslUsername = null, string? saslPassword = null)
    {
        _logger = logger;
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        if (!string.IsNullOrEmpty(saslUsername))
        {
            config.SecurityProtocol = SecurityProtocol.SaslPlaintext;
            config.SaslMechanism = SaslMechanism.ScramSha256;
            config.SaslUsername = saslUsername;
            config.SaslPassword = saslPassword;
        }
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(ProductEvent @event, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(@event);
            var message = new Message<string, string> { Key = @event.Product.Id.ToString(), Value = json };
            await _producer.ProduceAsync(Topic, message, ct);
            _logger.LogInformation("Event published: type={EventType}, productId={ProductId}",
                @event.EventType, @event.Product.Id);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: a publish failure never propagates to the caller, matching the
            // Quarkus/Spring siblings' KafkaProductEventPublisher behavior.
            _logger.LogError(ex, "Failed to publish event: type={EventType}", @event.EventType);
        }
    }

    public void Dispose() => _producer.Dispose();
}
