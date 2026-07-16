using Microsoft.Extensions.Logging;
using ProductApi.Domain;
using ProductApi.Domain.Events;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Ports;

namespace ProductApi.Application;

public sealed class ProductService(
    IProductRepository repository,
    IProductEventPublisher eventPublisher,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<Product> CreateProductAsync(Product product, CancellationToken ct = default)
    {
        var existing = await repository.FindBySkuAsync(product.Sku, ct);
        if (existing is not null)
            throw new DuplicateSkuException(product.Sku);

        var saved = await repository.SaveAsync(product, ct);
        await PublishSafelyAsync(ProductEvent.Of(ProductEventType.ProductCreated, saved), ct);
        logger.LogInformation("Product created — id={Id}", saved.Id);
        return saved;
    }

    public async Task<Product> FindByIdAsync(int id, CancellationToken ct = default) =>
        await repository.FindByIdAsync(id, ct) ?? throw new ProductNotFoundException(id);

    public Task<Product?> FindBySkuAsync(string sku, CancellationToken ct = default) =>
        repository.FindBySkuAsync(sku, ct);

    public async Task<(IReadOnlyList<Product> Items, long Total)> ListActiveProductsAsync(int page, int size, CancellationToken ct = default)
    {
        var items = await repository.FindAllActiveAsync(page, size, ct);
        var total = await repository.CountActiveAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Product> Items, long Total)> ListInactiveProductsAsync(int page, int size, CancellationToken ct = default)
    {
        var items = await repository.FindAllInactiveAsync(page, size, ct);
        var total = await repository.CountInactiveAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Product> Items, long Total)> SearchByNamePrefixAsync(string prefix, int page, int size, CancellationToken ct = default)
    {
        var items = await repository.SearchByNamePrefixAsync(prefix, page, size, ct);
        var total = await repository.CountByNamePrefixAsync(prefix, ct);
        return (items, total);
    }

    public Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default) =>
        repository.FindAllAsync(ct);

    public async Task<Product> UpdateProductAsync(int id, Product updatedData, CancellationToken ct = default)
    {
        _ = await repository.FindByIdAsync(id, ct) ?? throw new ProductNotFoundException(id);

        var toSave = new Product(updatedData.Sku, updatedData.Name, updatedData.Description,
            updatedData.Category, updatedData.Price, updatedData.Stock, updatedData.Active) { Id = id };
        var updated = await repository.UpdateAsync(toSave, ct);
        await PublishSafelyAsync(ProductEvent.Of(ProductEventType.ProductUpdated, updated), ct);
        logger.LogInformation("Product updated — id={Id}", updated.Id);
        return updated;
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var existing = await repository.FindByIdAsync(id, ct) ?? throw new ProductNotFoundException(id);
        await repository.DeleteAsync(id, ct);
        await PublishSafelyAsync(ProductEvent.Of(ProductEventType.ProductDeleted, existing), ct);
        logger.LogInformation("Product deleted — id={Id}", id);
    }

    private async Task PublishSafelyAsync(ProductEvent @event, CancellationToken ct)
    {
        try
        {
            await eventPublisher.PublishAsync(@event, ct);
        }
        catch (Exception ex)
        {
            // Event publish failures never fail the calling business operation — matches the
            // fire-and-forget philosophy of the Quarkus/Spring siblings' Kafka publishers.
            logger.LogError(ex, "Failed to publish event: type={EventType}, productId={ProductId}",
                @event.EventType, @event.Product.Id);
        }
    }
}
