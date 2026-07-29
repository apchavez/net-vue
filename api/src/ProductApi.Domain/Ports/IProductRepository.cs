namespace ProductApi.Domain.Ports;

public interface IProductRepository
{
    Task<Product> SaveAsync(Product product, CancellationToken ct = default);
    Task<Product> UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<Product?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> FindBySkuAsync(string sku, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindAllActiveAsync(int page, int size, CancellationToken ct = default);
    Task<long> CountActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindAllInactiveAsync(int page, int size, CancellationToken ct = default);
    Task<long> CountInactiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> SearchByNamePrefixAsync(string prefix, int page, int size, CancellationToken ct = default);
    Task<long> CountByNamePrefixAsync(string prefix, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindAllAsync(CancellationToken ct = default);
}
