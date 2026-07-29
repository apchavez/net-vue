using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductApi.Domain;
using ProductApi.Domain.Ports;
using ProductApi.Infrastructure.Persistence;
using StackExchange.Redis;
using Xunit;

namespace ProductApi.UnitTests;

public class CachedProductRepositoryTests
{
    private const string VersionKey = "products-active-cache:version";

    private readonly Mock<IProductRepository> _inner = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _database = new();
    private readonly CachedProductRepository _sut;

    public CachedProductRepositoryTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_database.Object);
        _sut = new CachedProductRepository(_inner.Object, _redis.Object, NullLogger<CachedProductRepository>.Instance);
    }

    private static Product Sample(int id = 1) => new("SKU-1", "Widget", null, null, 9.99m, 10, true) { Id = id };

    private void SetupNoVersionYet() =>
        _database.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k == VersionKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

    [Fact]
    public async Task FindAllActiveAsync_on_cache_miss_queries_inner_and_writes_cache()
    {
        SetupNoVersionYet();
        _database.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k != VersionKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        _inner.Setup(r => r.FindAllActiveAsync(0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample() });

        var result = await _sut.FindAllActiveAsync(0, 20);

        Assert.Single(result);
        _inner.Verify(r => r.FindAllActiveAsync(0, 20, It.IsAny<CancellationToken>()), Times.Once);
        _database.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(k => k != VersionKey), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task FindAllActiveAsync_on_cache_hit_does_not_call_inner()
    {
        SetupNoVersionYet();
        var cachedJson = JsonSerializer.Serialize(new List<Product> { Sample() });
        _database.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k != VersionKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)cachedJson);

        var result = await _sut.FindAllActiveAsync(0, 20);

        Assert.Single(result);
        Assert.Equal("SKU-1", result[0].Sku);
        _inner.Verify(r => r.FindAllActiveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindAllActiveAsync_falls_back_to_inner_when_redis_unavailable()
    {
        _database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));
        _database.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));
        _inner.Setup(r => r.FindAllActiveAsync(0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample() });

        var result = await _sut.FindAllActiveAsync(0, 20);

        Assert.Single(result);
        _inner.Verify(r => r.FindAllActiveAsync(0, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountActiveAsync_on_cache_hit_does_not_call_inner()
    {
        SetupNoVersionYet();
        _database.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k != VersionKey), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)"7");

        var result = await _sut.CountActiveAsync();

        Assert.Equal(7, result);
        _inner.Verify(r => r.CountActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("save")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Writes_invalidate_the_cache_by_bumping_the_version_counter(string operation)
    {
        _inner.Setup(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(Sample());
        _inner.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(Sample());
        _inner.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        switch (operation)
        {
            case "save": await _sut.SaveAsync(Sample()); break;
            case "update": await _sut.UpdateAsync(Sample()); break;
            case "delete": await _sut.DeleteAsync(1); break;
        }

        _database.Verify(d => d.StringIncrementAsync(
            It.Is<RedisKey>(k => k == VersionKey), It.IsAny<long>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task Invalidation_fails_open_when_redis_unavailable()
    {
        _database.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));
        _inner.Setup(r => r.SaveAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.SaveAsync(Sample());

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task FindByIdAsync_passes_through_uncached()
    {
        _inner.Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Sample());

        var result = await _sut.FindByIdAsync(1);

        Assert.Equal(1, result!.Id);
        _database.Verify(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task FindAllInactiveAsync_passes_through_uncached()
    {
        _inner.Setup(r => r.FindAllInactiveAsync(0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { Sample() });

        var result = await _sut.FindAllInactiveAsync(0, 20);

        Assert.Single(result);
        _inner.Verify(r => r.FindAllInactiveAsync(0, 20, It.IsAny<CancellationToken>()), Times.Once);
        _database.Verify(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
        _database.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task CountInactiveAsync_passes_through_uncached()
    {
        _inner.Setup(r => r.CountInactiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await _sut.CountInactiveAsync();

        Assert.Equal(3, result);
        _inner.Verify(r => r.CountInactiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _database.Verify(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }
}
