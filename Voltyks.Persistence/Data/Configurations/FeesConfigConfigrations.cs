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
    public class FeesConfigConfigrations : IEntityTypeConfiguration<FeesConfig>
    {
        public void Configure(EntityTypeBuilder<FeesConfig> builder)
        {
  
            builder.ToTable("FeesConfigs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MinimumFee)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();
            builder.Property(x => x.Percentage)
                   .HasColumnType("decimal(5,2)")
                   .IsRequired();
            builder.Property(x => x.UpdatedAt)
                   .HasColumnType("datetime2")
                   .IsRequired();
            builder.Property(x => x.UpdatedBy)
                   .HasMaxLength(128);

            builder.HasData(new FeesConfig
            {
                Id = 1,
                MinimumFee = 40m,
                Percentage = 10m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "system"
            });
        }
    }
}
