using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
    {
        public void Configure(EntityTypeBuilder<AppSettings> builder)
        {
            builder.ToTable("AppSettings");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ChargingModeEnabled)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(x => x.ChargingModeEnabledAt);

            builder.Property(x => x.UpdatedBy)
                   .HasMaxLength(450);

            builder.Property(x => x.UpdatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasData(new AppSettings
            {
                Id = 1,
                ChargingModeEnabled = false,
                ChargingModeEnabledAt = null,
                UpdatedBy = null,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
