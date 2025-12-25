using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main.Store;

namespace Voltyks.Persistence.Data.Configurations.Store
{
    public class StoreProductConfiguration : IEntityTypeConfiguration<StoreProduct>
    {
        public void Configure(EntityTypeBuilder<StoreProduct> builder)
        {
            builder.ToTable("StoreProducts");

            builder.Property(p => p.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(p => p.Slug)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(p => p.Description)
                   .HasMaxLength(2000);

            builder.Property(p => p.Price)
                   .IsRequired()
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Currency)
                   .IsRequired()
                   .HasMaxLength(10)
                   .HasDefaultValue("EGP");

            builder.Property(p => p.ImagesJson)
                   .HasColumnType("nvarchar(max)");

            builder.Property(p => p.SpecificationsJson)
                   .HasColumnType("nvarchar(max)");

            builder.Property(p => p.Status)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("active");

            // Relationships
            builder.HasOne(p => p.Category)
                   .WithMany(c => c.Products)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Reservations)
                   .WithOne(r => r.Product)
                   .HasForeignKey(r => r.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(p => p.Slug)
                   .IsUnique()
                   .HasDatabaseName("IX_StoreProducts_Slug");

            builder.HasIndex(p => new { p.CategoryId, p.IsDeleted, p.Status })
                   .HasDatabaseName("IX_StoreProducts_CategoryId_IsDeleted_Status");
        }
    }
}
