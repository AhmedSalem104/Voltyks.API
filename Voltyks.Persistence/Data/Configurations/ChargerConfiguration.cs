
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
            builder.HasOne(c => c.User)
                   .WithMany()
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(c => c.UserId).IsRequired();
            builder.Property(c => c.ProtocolId).IsRequired();
            builder.Property(c => c.CapacityId).IsRequired();
            builder.Property(c => c.PriceOptionId).IsRequired();
            builder.Property(c => c.AddressId).IsRequired();
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.DateAdded).IsRequired();
            builder.Property(c => c.IsDeleted).IsRequired();
        }
    }

}
