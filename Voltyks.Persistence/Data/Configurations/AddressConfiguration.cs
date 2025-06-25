using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class AddressConfiguration : IEntityTypeConfiguration<ChargerAddress>
    {
        public void Configure(EntityTypeBuilder<ChargerAddress> builder)
        {
            builder.Property(a => a.Area)
                  .IsRequired()
                  .HasMaxLength(100);

            builder.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.BuildingNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(a => a.Latitude)
                .IsRequired();

            builder.Property(a => a.Longitude)
                .IsRequired();


        }
    }
}
