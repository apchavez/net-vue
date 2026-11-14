using ProductApi.Domain.Exceptions;

namespace ProductApi.Domain;

public sealed record Product
{
    public int Id { get; init; }
    public string Sku { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? Category { get; }
    public decimal Price { get; }
    public int Stock { get; }
    public bool Active { get; }

    public Product(string sku, string name, string? description, string? category, decimal price, int stock, bool active)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new InvalidProductException("SKU must not be blank");
        if (sku.Length > 64)
            throw new InvalidProductException("SKU must not exceed 64 characters");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidProductException("Name must not be blank");
        if (name.Length > 200)
            throw new InvalidProductException("Name must not exceed 200 characters");
        if (description is { Length: > 1000 })
            throw new InvalidProductException("Description must not exceed 1000 characters");
        if (category is { Length: > 100 })
            throw new InvalidProductException("Category must not exceed 100 characters");
        if (price < 0)
            throw new InvalidProductException("Price must be greater than or equal to zero");
        if (stock < 0)
            throw new InvalidProductException("Stock must be greater than or equal to zero");

        Sku = sku;
        Name = name;
        Description = description;
        Category = category;
        Price = price;
        Stock = stock;
        Active = active;
    }
}
