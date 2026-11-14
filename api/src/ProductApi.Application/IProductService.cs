using ProductApi.Domain;

namespace ProductApi.Application;

public interface IProductService
{
    Task<Product> CreateProductAsync(Product product, CancellationToken ct = default);
    Task<Product> UpdateProductAsync(int id, Product updatedData, CancellationToken ct = default);
    Task DeleteProductAsync(int id, CancellationToken ct = default);
    Task<Product> FindByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> FindBySkuAsync(string sku, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, long Total)> ListActiveProductsAsync(int page, int size, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, long Total)> ListInactiveProductsAsync(int page, int size, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, long Total)> SearchByNamePrefixAsync(string prefix, int page, int size, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default);
}
