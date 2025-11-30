using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class UserGeneralComplaintConfiguration : IEntityTypeConfiguration<UserGeneralComplaint>
    {
        public void Configure(EntityTypeBuilder<UserGeneralComplaint> builder)
        {
            builder.ToTable("UserGeneralComplaints");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.UserId)
                   .IsRequired();

            builder.Property(c => c.CategoryId)
                   .IsRequired();

            builder.Property(c => c.Content)
                   .IsRequired()
                   .HasMaxLength(2000);

            builder.Property(c => c.CreatedAt)
                   .IsRequired();

            builder.Property(c => c.IsResolved)
                   .IsRequired()
                   .HasDefaultValue(false);

            // Relationship with User
            builder.HasOne(c => c.User)
                   .WithMany()
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Category
            builder.HasOne(c => c.Category)
                   .WithMany(cat => cat.Complaints)
                   .HasForeignKey(c => c.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Performance Indexes
            builder.HasIndex(c => new { c.UserId, c.IsResolved })
                   .HasDatabaseName("IX_UserGeneralComplaints_UserId_IsResolved");

            builder.HasIndex(c => c.CategoryId)
                   .HasDatabaseName("IX_UserGeneralComplaints_CategoryId");
        }
    }
}
