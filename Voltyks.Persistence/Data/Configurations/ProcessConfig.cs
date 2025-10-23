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
    public class ProcessConfig : IEntityTypeConfiguration<Process>
    {
        public void Configure(EntityTypeBuilder<Process> b)
        {
            b.ToTable("Process");
            b.HasKey(x => x.Id);

            b.Property(x => x.VehicleOwnerId).IsRequired();
            b.Property(x => x.ChargerOwnerId).IsRequired();
            b.Property(x => x.Status).HasConversion<int>();

            //b.HasIndex(x => x.ChargerRequestId).IsUnique(); // 1:1 مع الطلب
        }
    }
}
