using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
    {
        public void Configure(EntityTypeBuilder<WalletTransaction> builder)
        {
            builder.HasOne(t => t.User)
                   .WithMany(u => u.WalletTransactions)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Amount precision
            builder.Property(t => t.Amount)
                   .HasColumnType("decimal(18,2)");

            // Performance Indexes
            builder.HasIndex(t => t.UserId)
                   .HasDatabaseName("IX_WalletTransactions_UserId");

            builder.HasIndex(t => t.CreatedAt)
                   .HasDatabaseName("IX_WalletTransactions_CreatedAt");

            builder.HasIndex(t => new { t.UserId, t.CreatedAt })
                   .HasDatabaseName("IX_WalletTransactions_UserId_CreatedAt");
        }
    }
}
