using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main.Paymob;

namespace Voltyks.Persistence.Data.Configurations
{
    public class CardTokenWebhookLogConfiguration : IEntityTypeConfiguration<CardTokenWebhookLog>
    {
        public void Configure(EntityTypeBuilder<CardTokenWebhookLog> builder)
        {
            builder.ToTable("CardTokenWebhookLogs");
            builder.HasKey(x => x.Id);

            // Idempotency: Unique constraint on WebhookId
            builder.HasIndex(x => x.WebhookId).IsUnique();

            builder.Property(x => x.WebhookId).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserId).HasMaxLength(450);
            builder.Property(x => x.CardToken).HasMaxLength(500);
            builder.Property(x => x.Last4).HasMaxLength(4);
            builder.Property(x => x.Brand).HasMaxLength(50);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.FailureReason).HasMaxLength(500);
            builder.Property(x => x.RawPayload).IsRequired();
            builder.Property(x => x.ReceivedAt).HasDefaultValueSql("GETUTCDATE()");

            // FK to UserSavedCard
            builder.HasOne(x => x.SavedCard)
                   .WithMany()
                   .HasForeignKey(x => x.SavedCardId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Indexes for querying
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ReceivedAt);
        }
    }
}
