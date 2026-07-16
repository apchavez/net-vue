using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductApi.Domain;
using ProductApi.Domain.Ports;
using StackExchange.Redis;

namespace ProductApi.Infrastructure.Persistence;

/// <summary>
/// Cache-aside decorator around the active-products list read, mirroring the Quarkus/Spring
/// siblings' Redis cache-aside behavior (~5 min TTL, fail-open on Redis errors, invalidated on
/// writes) so all 3 repos behave identically for the paginated "active products" endpoint.
/// Every other <see cref="IProductRepository"/> method passes straight through to
/// <paramref name="inner"/> uncached — only the list read was flagged for caching.
///
/// Invalidation uses a version counter (INCR'd on every write) folded into the cache key, instead
/// of a Redis KEYS/SCAN pattern delete: page/size combinations are unbounded, so a version bump
/// is a single O(1) write that invalidates every existing page/size key at once (stale entries
/// simply age out via TTL) rather than requiring server-admin access to enumerate keys.
/// </summary>
public sealed class CachedProductRepository(
    IProductRepository inner,
    IConnectionMultiplexer redis,
    ILogger<CachedProductRepository> logger) : IProductRepository
{
    private const string KeyPrefix = "products-active-cache:";
    private const string VersionKey = $"{KeyPrefix}version";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<Product> SaveAsync(Product product, CancellationToken ct = default)
    {
        var saved = await inner.SaveAsync(product, ct);
        await InvalidateAsync();
        return saved;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken ct = default)
    {
        var updated = await inner.UpdateAsync(product, ct);
        await InvalidateAsync();
        return updated;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await InvalidateAsync();
    }

    public Task<Product?> FindByIdAsync(int id, CancellationToken ct = default) =>
        inner.FindByIdAsync(id, ct);

    public Task<Product?> FindBySkuAsync(string sku, CancellationToken ct = default) =>
        inner.FindBySkuAsync(sku, ct);

    public Task<IReadOnlyList<Product>> SearchByNamePrefixAsync(string prefix, int page, int size, CancellationToken ct = default) =>
        inner.SearchByNamePrefixAsync(prefix, page, size, ct);

    // Low-traffic admin-only view — intentionally NOT cached, unlike FindAllActiveAsync/CountActiveAsync above.
    public Task<IReadOnlyList<Product>> FindAllInactiveAsync(int page, int size, CancellationToken ct = default) =>
        inner.FindAllInactiveAsync(page, size, ct);

    public Task<long> CountInactiveAsync(CancellationToken ct = default) =>
        inner.CountInactiveAsync(ct);

    public Task<long> CountByNamePrefixAsync(string prefix, CancellationToken ct = default) =>
        inner.CountByNamePrefixAsync(prefix, ct);

    // Report generation reads the full catalog once per request — same "not cached" reasoning
    // as FindAllInactiveAsync above (low-traffic admin/reporting path, not the hot list view).
    public Task<IReadOnlyList<Product>> FindAllAsync(CancellationToken ct = default) =>
        inner.FindAllAsync(ct);

    public async Task<IReadOnlyList<Product>> FindAllActiveAsync(int page, int size, CancellationToken ct = default)
    {
        var key = await BuildKeyAsync("items", page, size);

        if (key is not null)
        {
            try
            {
                var cached = await redis.GetDatabase().StringGetAsync(key);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<List<Product>>((string)cached!) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CACHE] Redis unavailable (fail-open) reading active-products list — page={Page}, size={Size}", page, size);
            }
        }

        var fresh = await inner.FindAllActiveAsync(page, size, ct);

        if (key is not null)
        {
            try
            {
                await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(fresh), Ttl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CACHE] Redis unavailable (fail-open) writing active-products list cache — page={Page}, size={Size}", page, size);
            }
        }

        return fresh;
    }

    public async Task<long> CountActiveAsync(CancellationToken ct = default)
    {
        var key = await BuildKeyAsync("count", 0, 0);

        if (key is not null)
        {
            try
            {
                var cached = await redis.GetDatabase().StringGetAsync(key);
                if (cached.HasValue && long.TryParse((string?)cached, out var count))
                    return count;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CACHE] Redis unavailable (fail-open) reading active-products count");
            }
        }

        var fresh = await inner.CountActiveAsync(ct);

        if (key is not null)
        {
            try
            {
                await redis.GetDatabase().StringSetAsync(key, fresh, Ttl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[CACHE] Redis unavailable (fail-open) writing active-products count cache");
            }
        }

        return fresh;
    }

    /// <returns>null if Redis is unreachable — callers skip caching entirely for that call (fail-open).</returns>
    private async Task<string?> BuildKeyAsync(string kind, int page, int size)
    {
        try
        {
            var version = await redis.GetDatabase().StringGetAsync(VersionKey);
            var v = version.HasValue && long.TryParse((string?)version, out var parsed) ? parsed : 0L;
            return $"{KeyPrefix}v{v}:{kind}:{page}:{size}";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CACHE] Redis unavailable (fail-open) building cache key");
            return null;
        }
    }

    private async Task InvalidateAsync()
    {
        try
        {
            await redis.GetDatabase().StringIncrementAsync(VersionKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CACHE] Redis unavailable (fail-open) invalidating active-products cache");
        }
    }
}
