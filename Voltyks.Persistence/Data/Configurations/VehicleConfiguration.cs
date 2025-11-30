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
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.Property(v => v.Color)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(v => v.Plate)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(v => v.CreationDate)
                   .IsRequired();

            builder.Property(v => v.BrandId)
                   .IsRequired();

            builder.Property(v => v.ModelId)
                   .IsRequired();

            builder.Property(v => v.UserId)
                   .IsRequired();

            builder.HasOne(v => v.Brand)
                   .WithMany()
                   .HasForeignKey(v => v.BrandId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.Model)
                   .WithMany()
                   .HasForeignKey(v => v.ModelId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.User)
                   .WithMany()
                   .HasForeignKey(v => v.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Performance Indexes
            builder.HasIndex(v => new { v.UserId, v.IsDeleted })
                   .HasDatabaseName("IX_Vehicles_UserId_IsDeleted");

            builder.HasIndex(v => v.Plate)
                   .HasDatabaseName("IX_Vehicles_Plate");
        }
    }
}
