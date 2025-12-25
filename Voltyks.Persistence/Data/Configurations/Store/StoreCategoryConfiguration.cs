using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main.Store;

namespace Voltyks.Persistence.Data.Configurations.Store
{
    public class StoreCategoryConfiguration : IEntityTypeConfiguration<StoreCategory>
    {
        public void Configure(EntityTypeBuilder<StoreCategory> builder)
        {
            builder.ToTable("StoreCategories");

            builder.Property(c => c.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Slug)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Status)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("active");

            builder.Property(c => c.Icon)
                   .HasMaxLength(255);

            builder.Property(c => c.PlaceholderMessage)
                   .HasMaxLength(500);

            // Relationships
            builder.HasMany(c => c.Products)
                   .WithOne(p => p.Category)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(c => c.Slug)
                   .IsUnique()
                   .HasDatabaseName("IX_StoreCategories_Slug");

            builder.HasIndex(c => new { c.IsDeleted, c.Status, c.SortOrder })
                   .HasDatabaseName("IX_StoreCategories_IsDeleted_Status_SortOrder");
        }
    }
}
