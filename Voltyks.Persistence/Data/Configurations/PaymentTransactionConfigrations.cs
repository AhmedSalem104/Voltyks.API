using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.Persistence.Data.Configurations
{
    internal class PaymentOrderConfigrations : IEntityTypeConfiguration<PaymentTransaction>
    {
      

        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
           
            builder
              .HasIndex(x => x.PaymobTransactionId);

            builder
              .HasOne(t => t.Order)
              .WithMany(o => o.Transactions)
              .HasForeignKey(t => t.MerchantOrderId)
              .HasPrincipalKey(o => o.MerchantOrderId);
        }
    }
}
