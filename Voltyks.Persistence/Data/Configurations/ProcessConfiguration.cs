using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class ProcessConfiguration : IEntityTypeConfiguration<Process>
    {
        public void Configure(EntityTypeBuilder<Process> builder)
        {
            builder.ToTable("Process");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.VehicleOwnerId)
                   .IsRequired();

            builder.Property(p => p.ChargerOwnerId)
                   .IsRequired();

            builder.Property(p => p.EstimatedPrice)
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.AmountPaid)
                   .HasColumnType("decimal(18,2)");

            builder.Property(p => p.AmountCharged)
                   .HasColumnType("decimal(18,2)");

            // Performance Indexes
            builder.HasIndex(p => new { p.VehicleOwnerId, p.Status })
                   .HasDatabaseName("IX_Processes_VehicleOwnerId_Status");

            builder.HasIndex(p => new { p.ChargerOwnerId, p.Status })
                   .HasDatabaseName("IX_Processes_ChargerOwnerId_Status");

            builder.HasIndex(p => p.ChargerRequestId)
                   .HasDatabaseName("IX_Processes_ChargerRequestId");
        }
    }
}
