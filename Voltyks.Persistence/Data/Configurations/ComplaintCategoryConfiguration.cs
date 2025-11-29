using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class ComplaintCategoryConfiguration : IEntityTypeConfiguration<ComplaintCategory>
    {
        public void Configure(EntityTypeBuilder<ComplaintCategory> builder)
        {
            builder.ToTable("ComplaintCategories");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(c => c.Description)
                   .HasMaxLength(500);

            builder.Property(c => c.CreatedAt)
                   .IsRequired();

            builder.Property(c => c.IsDeleted)
                   .IsRequired()
                   .HasDefaultValue(false);

            // Relationship with Complaints
            builder.HasMany(c => c.Complaints)
                   .WithOne(uc => uc.Category)
                   .HasForeignKey(uc => uc.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
