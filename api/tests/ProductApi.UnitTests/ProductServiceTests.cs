using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductApi.Application;
using ProductApi.Domain;
using ProductApi.Domain.Events;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Ports;
using Xunit;

namespace ProductApi.UnitTests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IProductEventPublisher> _eventPublisher = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repository.Object, _eventPublisher.Object, NullLogger<ProductService>.Instance);
    }

    private static Product Sample(int id = 1) => new("SKU-1", "Widget", null, null, 9.99m, 10, true) { Id = id };

    [Fact]
    public async Task CreateProductAsync_saves_and_publishes_created_event()
    {
        _repository.Setup(r => r.FindBySkuAsync("SKU-1", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _repository.Setup(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.CreateProductAsync(Sample(0));

        Assert.Equal(1, result.Id);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<ProductEvent>(e => e.EventType == ProductEventType.ProductCreated), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_throws_DuplicateSkuException_when_sku_exists()
    {
        _repository.Setup(r => r.FindBySkuAsync("SKU-1", It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        await Assert.ThrowsAsync<DuplicateSkuException>(() => _sut.CreateProductAsync(Sample(0)));
        _repository.Verify(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_does_not_throw_when_event_publish_fails()
    {
        _repository.Setup(r => r.FindBySkuAsync("SKU-1", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _repository.Setup(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(Sample());
        _eventPublisher.Setup(p => p.PublishAsync(It.IsAny<ProductEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kafka down"));

        var result = await _sut.CreateProductAsync(Sample(0));

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task FindByIdAsync_throws_NotFound_when_missing()
    {
        _repository.Setup(r => r.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<ProductNotFoundException>(() => _sut.FindByIdAsync(99));
    }

    [Fact]
    public async Task FindByIdAsync_returns_product_when_found()
    {
        _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.FindByIdAsync(1);

        Assert.Equal("SKU-1", result.Sku);
    }

    [Fact]
    public async Task UpdateProductAsync_throws_NotFound_when_missing()
    {
        _repository.Setup(r => r.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<ProductNotFoundException>(() => _sut.UpdateProductAsync(99, Sample(99)));
    }

    [Fact]
    public async Task UpdateProductAsync_updates_and_publishes_updated_event()
    {
        _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.UpdateProductAsync(1, Sample());

        Assert.Equal(1, result.Id);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<ProductEvent>(e => e.EventType == ProductEventType.ProductUpdated), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_throws_NotFound_when_missing()
    {
        _repository.Setup(r => r.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<ProductNotFoundException>(() => _sut.DeleteProductAsync(99));
    }

    [Fact]
    public async Task DeleteProductAsync_deletes_and_publishes_deleted_event()
    {
        _repository.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        await _sut.DeleteProductAsync(1);

        _repository.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<ProductEvent>(e => e.EventType == ProductEventType.ProductDeleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindBySkuAsync_returns_null_when_not_found()
    {
        _repository.Setup(r => r.FindBySkuAsync("MISSING", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _sut.FindBySkuAsync("MISSING");

        Assert.Null(result);
    }

    [Fact]
    public async Task ListActiveProductsAsync_returns_items_and_total()
    {
        _repository.Setup(r => r.FindAllActiveAsync(0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample() });
        _repository.Setup(r => r.CountActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var (items, total) = await _sut.ListActiveProductsAsync(0, 20);

        Assert.Single(items);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task ListInactiveProductsAsync_returns_items_and_total()
    {
        _repository.Setup(r => r.FindAllInactiveAsync(0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample() });
        _repository.Setup(r => r.CountInactiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var (items, total) = await _sut.ListInactiveProductsAsync(0, 20);

        Assert.Single(items);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task SearchByNamePrefixAsync_returns_items_and_total()
    {
        _repository.Setup(r => r.SearchByNamePrefixAsync("Wid", 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample() });
        _repository.Setup(r => r.CountByNamePrefixAsync("Wid", It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var (items, total) = await _sut.SearchByNamePrefixAsync("Wid", 0, 20);

        Assert.Single(items);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetAllProductsAsync_returns_full_catalog_from_repository()
    {
        _repository.Setup(r => r.FindAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample(1), Sample(2) });

        var result = await _sut.GetAllProductsAsync();

        Assert.Equal(2, result.Count);
        _repository.Verify(r => r.FindAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
