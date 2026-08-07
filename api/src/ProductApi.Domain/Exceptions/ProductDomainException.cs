namespace ProductApi.Domain.Exceptions;

public abstract class ProductDomainException(string message) : Exception(message);

public sealed class InvalidProductException(string message) : ProductDomainException(message);

public sealed class ProductNotFoundException(int id) : ProductDomainException($"Product not found with id: {id}")
{
    public int Id { get; } = id;
}

public sealed class DuplicateSkuException(string sku)
    : ProductDomainException($"A product with SKU '{sku}' already exists")
{
    public string Sku { get; } = sku;
}
