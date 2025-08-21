using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.Persistence.Data.Configurations
{
    internal class PaymobConfigrations : IEntityTypeConfiguration<PaymentOrder>
    {
      

        public void Configure(EntityTypeBuilder<PaymentOrder> builder)
        {
            builder
              .HasIndex(x => x.MerchantOrderId)
              .IsUnique();

         
        }
    }
}
