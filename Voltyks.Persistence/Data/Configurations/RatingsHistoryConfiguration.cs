using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class RatingsHistoryConfiguration : IEntityTypeConfiguration<RatingsHistory>
    {
        public void Configure(EntityTypeBuilder<RatingsHistory> builder)
        {
            builder.ToTable("RatingsHistory");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ProcessId)
                   .IsRequired();

            builder.Property(r => r.RaterUserId)
                   .IsRequired();

            builder.Property(r => r.RateeUserId)
                   .IsRequired();

            builder.Property(r => r.Stars)
                   .IsRequired();

            builder.Property(r => r.CreatedAt)
                   .IsRequired();

            // Performance Indexes
            builder.HasIndex(r => r.ProcessId)
                   .HasDatabaseName("IX_RatingsHistory_ProcessId");

            builder.HasIndex(r => r.RaterUserId)
                   .HasDatabaseName("IX_RatingsHistory_RaterUserId");

            builder.HasIndex(r => r.RateeUserId)
                   .HasDatabaseName("IX_RatingsHistory_RateeUserId");
        }
    }
}
