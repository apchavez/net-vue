using Microsoft.EntityFrameworkCore;

namespace ProductApi.Infrastructure.Persistence;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => e.Active);
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.Sku).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("numeric(12,2)");
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_product_price", "price >= 0");
                t.HasCheckConstraint("CK_product_stock", "stock >= 0");
            });
        });
    }
}
