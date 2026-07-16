using ProductApi.Domain;
using ProductApi.Domain.Exceptions;
using Xunit;

namespace ProductApi.UnitTests;

public class ProductTests
{
    private static Product Valid() => new("SKU-1", "Widget", "desc", "cat", 9.99m, 10, true);

    [Fact]
    public void Constructs_valid_product()
    {
        var product = Valid();
        Assert.Equal("SKU-1", product.Sku);
        Assert.True(product.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Throws_when_sku_blank(string? sku)
    {
        Assert.Throws<InvalidProductException>(() => new Product(sku!, "Widget", null, null, 1m, 1, true));
    }

    [Fact]
    public void Throws_when_sku_too_long()
    {
        var longSku = new string('a', 65);
        Assert.Throws<InvalidProductException>(() => new Product(longSku, "Widget", null, null, 1m, 1, true));
    }

    [Fact]
    public void Throws_when_name_blank()
    {
        Assert.Throws<InvalidProductException>(() => new Product("SKU-1", "", null, null, 1m, 1, true));
    }

    [Fact]
    public void Throws_when_name_too_long()
    {
        var longName = new string('a', 201);
        Assert.Throws<InvalidProductException>(() => new Product("SKU-1", longName, null, null, 1m, 1, true));
    }

    [Fact]
    public void Throws_when_description_too_long()
    {
        var longDesc = new string('a', 1001);
        Assert.Throws<InvalidProductException>(() => new Product("SKU-1", "Widget", longDesc, null, 1m, 1, true));
    }

    [Fact]
    public void Throws_when_category_too_long()
    {
        var longCategory = new string('a', 101);
        Assert.Throws<InvalidProductException>(() => new Product("SKU-1", "Widget", null, longCategory, 1m, 1, true));
    }

    [Fact]
    public void Throws_when_price_negative()
    {
        Assert.Throws<InvalidProductException>(() => new Product("SKU-1", "Widget", null, null, -1m, 1, true));
    }

    [Fact]
    public void Throws_when_stock_negative()
    {
        Assert.Throws<InvalidProductException>(() => new Product("SKU-1", "Widget", null, null, 1m, -1, true));
    }

    [Fact]
    public void Allows_zero_price_and_stock()
    {
        var product = new Product("SKU-1", "Widget", null, null, 0m, 0, true);
        Assert.Equal(0m, product.Price);
        Assert.Equal(0, product.Stock);
    }
}
