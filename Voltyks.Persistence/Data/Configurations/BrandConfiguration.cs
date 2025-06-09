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
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.Property(b => b.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            // Relationships
            builder.HasMany<Model>()
                   .WithOne(m => m.Brand)
                   .HasForeignKey(m => m.BrandId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany<Vehicle>()
                   .WithOne(v => v.Brand)
                   .HasForeignKey(v => v.BrandId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
