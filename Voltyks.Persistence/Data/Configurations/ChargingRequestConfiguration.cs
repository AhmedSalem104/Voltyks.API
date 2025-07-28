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
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Charger)
                   .WithMany(c => c.ChargingRequests)
                   .HasForeignKey(r => r.ChargerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
