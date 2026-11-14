using System.Text;
using ClosedXML.Excel;
using ProductApi.Api.Reports;
using ProductApi.Domain;
using Xunit;

namespace ProductApi.UnitTests;

public class ProductReportGeneratorTests
{
    static ProductReportGeneratorTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static readonly List<Product> Products =
    [
        new("SKU-1", "Widget", "desc", "Tools", 9.99m, 10, true),
        new("SKU-2", "Gadget", null, null, 5.00m, 2, false)
    ];

    [Fact]
    public void GeneratePdf_returns_a_valid_pdf_byte_stream()
    {
        var bytes = ProductReportGenerator.GeneratePdf(Products);

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void GenerateExcel_produces_a_workbook_with_matching_rows()
    {
        var bytes = ProductReportGenerator.GenerateExcel(Products);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("SKU", sheet.Cell(1, 1).GetString());
        Assert.Equal("SKU-1", sheet.Cell(2, 1).GetString());
        Assert.Equal("Widget", sheet.Cell(2, 2).GetString());
        Assert.Equal(9.99, sheet.Cell(2, 4).GetDouble(), 2);
        Assert.Equal("SKU-2", sheet.Cell(3, 1).GetString());
    }
}
