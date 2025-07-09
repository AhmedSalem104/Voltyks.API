
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;
using System.Reflection.Emit;

namespace Voltyks.Persistence.Data.Configurations
{
    public class ChargerConfiguration : IEntityTypeConfiguration<Charger>
    {
        public void Configure(EntityTypeBuilder<Charger> builder)
        {
            // الخصائص المطلوبة
            builder.Property(c => c.UserId).IsRequired();
            builder.Property(c => c.ProtocolId).IsRequired();
            builder.Property(c => c.CapacityId).IsRequired();
            builder.Property(c => c.PriceOptionId).IsRequired();
            builder.Property(c => c.AddressId).IsRequired();
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.DateAdded).IsRequired();
            builder.Property(c => c.IsDeleted).IsRequired();

            // العلاقات
            builder.HasOne(c => c.User)
                   .WithMany()
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Protocol)
                   .WithMany()
                   .HasForeignKey(c => c.ProtocolId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Capacity)
                   .WithMany()
                   .HasForeignKey(c => c.CapacityId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.PriceOption)
                   .WithMany()
                   .HasForeignKey(c => c.PriceOptionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Address)
                   .WithMany()
                   .HasForeignKey(c => c.AddressId)
                   .OnDelete(DeleteBehavior.Restrict);

          
        }
    }


}
