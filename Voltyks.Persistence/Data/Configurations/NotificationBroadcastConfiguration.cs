using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class NotificationBroadcastConfiguration : IEntityTypeConfiguration<NotificationBroadcast>
    {
        public void Configure(EntityTypeBuilder<NotificationBroadcast> builder)
        {
            builder.ToTable("NotificationBroadcasts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AdminUserId).HasMaxLength(450).IsRequired();
            builder.Property(x => x.AudienceJson).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            builder.Property(x => x.TemplateKey).HasMaxLength(120);

            builder.Property(x => x.SentAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(x => x.AdminUserId);
            builder.HasIndex(x => x.SentAt);
        }
    }
}
