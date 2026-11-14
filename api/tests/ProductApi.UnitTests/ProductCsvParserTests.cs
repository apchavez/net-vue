using ProductApi.Api.Reports;
using ProductApi.Domain.Exceptions;
using Xunit;

namespace ProductApi.UnitTests;

public class ProductCsvParserTests
{
    [Fact]
    public void ParseRow_parses_a_well_formed_row()
    {
        var product = ProductCsvParser.ParseRow("SKU-1,Widget,A nice widget,Tools,9.99,10,true");

        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("Widget", product.Name);
        Assert.Equal("A nice widget", product.Description);
        Assert.Equal("Tools", product.Category);
        Assert.Equal(9.99m, product.Price);
        Assert.Equal(10, product.Stock);
        Assert.True(product.Active);
    }

    [Fact]
    public void ParseRow_treats_blank_description_and_category_as_null()
    {
        var product = ProductCsvParser.ParseRow("SKU-1,Widget,,,9.99,10,true");

        Assert.Null(product.Description);
        Assert.Null(product.Category);
    }

    [Fact]
    public void ParseRow_handles_quoted_fields_containing_commas()
    {
        var product = ProductCsvParser.ParseRow("SKU-1,Widget,\"Nice, sturdy widget\",Tools,9.99,10,true");

        Assert.Equal("Nice, sturdy widget", product.Description);
    }

    [Theory]
    [InlineData("SKU-1,Widget,,,9.99,10")]
    [InlineData("SKU-1,Widget,,,9.99,10,true,extra")]
    public void ParseRow_throws_when_column_count_is_wrong(string line)
    {
        Assert.Throws<InvalidProductException>(() => ProductCsvParser.ParseRow(line));
    }

    [Fact]
    public void ParseRow_throws_on_invalid_price()
    {
        Assert.Throws<InvalidProductException>(() => ProductCsvParser.ParseRow("SKU-1,Widget,,,not-a-price,10,true"));
    }

    [Fact]
    public void ParseRow_throws_on_invalid_stock()
    {
        Assert.Throws<InvalidProductException>(() => ProductCsvParser.ParseRow("SKU-1,Widget,,,9.99,not-a-number,true"));
    }

    [Fact]
    public void ParseRow_throws_on_invalid_active_flag()
    {
        Assert.Throws<InvalidProductException>(() => ProductCsvParser.ParseRow("SKU-1,Widget,,,9.99,10,not-a-bool"));
    }

    [Fact]
    public void ParseRow_throws_on_blank_sku_via_domain_validation()
    {
        Assert.Throws<InvalidProductException>(() => ProductCsvParser.ParseRow(",Widget,,,9.99,10,true"));
    }
}
