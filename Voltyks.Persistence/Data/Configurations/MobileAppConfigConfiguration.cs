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

            builder.Property(x => x.MobileAppEnabled)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.HasData(new MobileAppConfig
            {
                Id = 1,
                MobileAppEnabled = true
            });
        }
    }
}
