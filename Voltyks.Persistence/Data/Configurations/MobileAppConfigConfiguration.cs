using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class MobileAppConfigConfiguration : IEntityTypeConfiguration<MobileAppConfig>
    {
        public void Configure(EntityTypeBuilder<MobileAppConfig> builder)
        {
            builder.ToTable("MobileAppConfigs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AndroidEnabled)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(x => x.IosEnabled)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(x => x.AndroidMinVersion)
                   .HasMaxLength(20);

            builder.Property(x => x.IosMinVersion)
                   .HasMaxLength(20);

            builder.HasData(new MobileAppConfig
            {
                Id = 1,
                AndroidEnabled = true,
                IosEnabled = true,
                AndroidMinVersion = null,
                IosMinVersion = null
            });
        }
    }
}
