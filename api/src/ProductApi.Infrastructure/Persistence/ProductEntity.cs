using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductApi.Infrastructure.Persistence;

[Table("product")]
public class ProductEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("sku")]
    public string Sku { get; set; } = null!;

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("stock")]
    public int Stock { get; set; }

    [Column("active")]
    public bool Active { get; set; }
}
