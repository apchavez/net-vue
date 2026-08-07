using Microsoft.EntityFrameworkCore;
using ProductApi.Domain;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Ports;

namespace ProductApi.Infrastructure.Persistence;

public sealed class ProductRepository(ProductDbContext dbContext) : IProductRepository
{
    public async Task<Product> SaveAsync(Product product, CancellationToken ct = default)
    {
        var entity = ToEntity(product);
        dbContext.Products.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return ToDomain(entity);
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken ct = default)
    {
        var entity = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id, ct)
                     ?? throw new ProductNotFoundException(product.Id);

        entity.Sku = product.Sku;
        entity.Name = product.Name;
        entity.Description = product.Description;
        entity.Category = product.Category;
        entity.Price = product.Price;
        entity.Stock = product.Stock;
        entity.Active = product.Active;

        await dbContext.SaveChangesAsync(ct);
        return ToDomain(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;
        dbContext.Products.Remove(entity);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<Product?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Product?> FindBySkuAsync(string sku, CancellationToken ct = default)
    {
        var entity = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Sku == sku, ct);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Product>> FindAllActiveAsync(int page, int size, CancellationToken ct = default)
    {
        var entities = await dbContext.Products.AsNoTracking()
            .Where(p => p.Active)
            .OrderBy(p => p.Id)
            .Skip(page * size)
            .Take(size)
            .ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public Task<long> CountActiveAsync(CancellationToken ct = default) =>
        dbContext.Products.AsNoTracking().LongCountAsync(p => p.Active, ct);

    public async Task<IReadOnlyList<Product>> FindAllInactiveAsync(int page, int size, CancellationToken ct = default)
    {
        var entities = await dbContext.Products.AsNoTracking()
            .Where(p => !p.Active)
            .OrderBy(p => p.Id)
            .Skip(page * size)
            .Take(size)
            .ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public Task<long> CountInactiveAsync(CancellationToken ct = default) =>
        dbContext.Products.AsNoTracking().LongCountAsync(p => !p.Active, ct);

    public async Task<IReadOnlyList<Product>> SearchByNamePrefixAsync(string prefix, int page, int size, CancellationToken ct = default)
    {
        var entities = await dbContext.Products.AsNoTracking()
            .Where(p => EF.Functions.ILike(p.Name, prefix + "%"))
            .OrderBy(p => p.Id)
            .Skip(page * size)
            .Take(size)
            .ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public Task<long> CountByNamePrefixAsync(string prefix, CancellationToken ct = default) =>
        dbContext.Products.AsNoTracking().LongCountAsync(p => EF.Functions.ILike(p.Name, prefix + "%"), ct);

    public async Task<IReadOnlyList<Product>> FindAllAsync(CancellationToken ct = default)
    {
        var entities = await dbContext.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    private static ProductEntity ToEntity(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        Description = product.Description,
        Category = product.Category,
        Price = product.Price,
        Stock = product.Stock,
        Active = product.Active
    };

    private static Product ToDomain(ProductEntity entity) => new(
        entity.Sku, entity.Name, entity.Description, entity.Category,
        entity.Price, entity.Stock, entity.Active)
    { Id = entity.Id };
}
