using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
    {
        public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
        {
            builder.ToTable("NotificationTemplates");
            builder.HasKey(x => x.Key);

            builder.Property(x => x.Key).HasMaxLength(120).IsRequired();
            builder.Property(x => x.TitleEn).HasMaxLength(255).IsRequired();
            builder.Property(x => x.TitleAr).HasMaxLength(255).IsRequired();
            builder.Property(x => x.BodyEn).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.BodyAr).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.RequiredParamsJson).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.IsCustomizable).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.UpdatedBy).HasMaxLength(450);

            builder.Property(x => x.UpdatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
