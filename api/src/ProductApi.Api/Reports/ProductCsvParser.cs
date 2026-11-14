using System.Globalization;
using System.Text;
using ProductApi.Domain;
using ProductApi.Domain.Exceptions;

namespace ProductApi.Api.Reports;

/// <summary>
/// Minimal CSV parser for the product import endpoint. Deliberately hand-rolled instead of a
/// dependency (CsvHelper etc.) — the format is a fixed 7-column shape, and this keeps the import
/// endpoint dependency-free beyond the two reporting libraries.
/// </summary>
public static class ProductCsvParser
{
    public static Product ParseRow(string line)
    {
        var fields = SplitLine(line);
        if (fields.Length != 7)
            throw new InvalidProductException(
                $"expected 7 columns (sku,name,description,category,price,stock,active), got {fields.Length}");

        var sku = fields[0].Trim();
        var name = fields[1].Trim();
        var description = fields[2].Trim();
        var category = fields[3].Trim();

        if (!decimal.TryParse(fields[4].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            throw new InvalidProductException($"invalid price: '{fields[4]}'");
        if (!int.TryParse(fields[5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock))
            throw new InvalidProductException($"invalid stock: '{fields[5]}'");
        if (!bool.TryParse(fields[6].Trim(), out var active))
            throw new InvalidProductException($"invalid active flag: '{fields[6]}'");

        return new Product(
            sku,
            name,
            string.IsNullOrWhiteSpace(description) ? null : description,
            string.IsNullOrWhiteSpace(category) ? null : category,
            price,
            stock,
            active);
    }

    private static string[] SplitLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
