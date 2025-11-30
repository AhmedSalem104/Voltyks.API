using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.Persistence.Data.Configurations
{
    public class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
    {
        public void Configure(EntityTypeBuilder<PaymentOrder> builder)
        {
            builder.ToTable("PaymentOrders");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.MerchantOrderId)
                   .IsRequired();

            builder.Property(p => p.UserId)
                   .IsRequired();

            builder.Property(p => p.AmountCents)
                   .IsRequired();

            builder.Property(p => p.Currency)
                   .HasMaxLength(10)
                   .HasDefaultValue("EGP");

            builder.Property(p => p.Status)
                   .HasMaxLength(50)
                   .HasDefaultValue("Pending");

            // Performance Indexes
            builder.HasIndex(p => new { p.UserId, p.Status })
                   .HasDatabaseName("IX_PaymentOrders_UserId_Status");

            builder.HasIndex(p => p.MerchantOrderId)
                   .HasDatabaseName("IX_PaymentOrders_MerchantOrderId");

            builder.HasIndex(p => p.PaymobOrderId)
                   .HasDatabaseName("IX_PaymentOrders_PaymobOrderId");
        }
    }
}
