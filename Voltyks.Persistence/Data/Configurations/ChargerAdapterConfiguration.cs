using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class ChargerAdapterConfiguration : IEntityTypeConfiguration<ChargerAdapter>
    {
        public void Configure(EntityTypeBuilder<ChargerAdapter> builder)
        {
            builder.ToTable("ChargerAdapters");

            builder.HasKey(ca => new { ca.ChargerId, ca.ProtocolId });

            builder.HasOne(ca => ca.Charger)
                   .WithMany(c => c.Adapters)
                   .HasForeignKey(ca => ca.ChargerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ca => ca.Protocol)
                   .WithMany()
                   .HasForeignKey(ca => ca.ProtocolId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ca => ca.ProtocolId)
                   .HasDatabaseName("IX_ChargerAdapters_ProtocolId");
        }
    }
}
