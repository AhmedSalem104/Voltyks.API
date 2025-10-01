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
    public class ChargingRequestConfiguration : IEntityTypeConfiguration<ChargingRequest>
    {
        public void Configure(EntityTypeBuilder<ChargingRequest> builder)
        {
            builder.HasOne(r => r.CarOwner)
                   .WithMany(u => u.ChargingRequests)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Charger)
                   .WithMany(c => c.ChargingRequests)
                   .HasForeignKey(r => r.ChargerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => new { r.Status, r.RequestedAt });

            builder.HasIndex(r => new { r.Status, r.RequestedAt })
                   .HasDatabaseName("IX_ChargingRequests_Status_RequestedAt");  

            builder.Property(r => r.Status)
                   .HasColumnType("nvarchar(450)")  
                   .IsRequired();

            builder.Property(x => x.BaseAmount)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(x => x.VoltyksFees)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(x => x.EstimatedPrice)
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);



        }
    }

}
