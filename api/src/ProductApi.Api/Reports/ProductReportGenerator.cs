using ClosedXML.Excel;
using ProductApi.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProductApi.Api.Reports;

public static class ProductReportGenerator
{
    private static readonly string[] Headers = ["SKU", "Name", "Category", "Price", "Stock", "Active"];

    public static byte[] GeneratePdf(IReadOnlyList<Product> products)
    {
        var totalValue = products.Sum(p => p.Price * p.Stock);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Header().Text("Products Report").FontSize(18).Bold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        foreach (var title in Headers)
                            header.Cell().Border(1).Padding(4).Text(title).Bold();
                    });

                    foreach (var p in products)
                    {
                        table.Cell().Border(1).Padding(4).Text(p.Sku);
                        table.Cell().Border(1).Padding(4).Text(p.Name);
                        table.Cell().Border(1).Padding(4).Text(p.Category ?? "-");
                        table.Cell().Border(1).Padding(4).Text(p.Price.ToString("0.00"));
                        table.Cell().Border(1).Padding(4).Text(p.Stock.ToString());
                        table.Cell().Border(1).Padding(4).Text(p.Active ? "Yes" : "No");
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().Text($"Total products: {products.Count}");
                    col.Item().Text($"Total inventory value: {totalValue:0.00}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public static byte[] GenerateExcel(IReadOnlyList<Product> products)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");

        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];
        sheet.Row(1).Style.Font.Bold = true;

        var row = 2;
        foreach (var p in products)
        {
            sheet.Cell(row, 1).Value = p.Sku;
            sheet.Cell(row, 2).Value = p.Name;
            sheet.Cell(row, 3).Value = p.Category ?? "-";
            sheet.Cell(row, 4).Value = p.Price;
            sheet.Cell(row, 5).Value = p.Stock;
            sheet.Cell(row, 6).Value = p.Active ? "Yes" : "No";
            row++;
        }

        var totalValue = products.Sum(p => p.Price * p.Stock);
        sheet.Cell(row + 1, 1).Value = "Total products:";
        sheet.Cell(row + 1, 2).Value = products.Count;
        sheet.Cell(row + 2, 1).Value = "Total inventory value:";
        sheet.Cell(row + 2, 2).Value = totalValue;

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
