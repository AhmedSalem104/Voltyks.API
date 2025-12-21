using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities.Main;
using System.Reflection.Emit;

namespace Voltyks.Persistence.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notifications)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired(false);

            builder.HasOne(n => n.RelatedRequest)
                   .WithMany()
                   .HasForeignKey(n => n.RelatedRequestId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasIndex(n => n.RelatedRequestId)
                   .HasDatabaseName("IX_Notifications_RelatedRequestId");

            // Admin notifications index
            builder.HasIndex(n => n.IsAdminNotification)
                   .HasDatabaseName("IX_Notifications_IsAdminNotification");

            builder.Property(n => n.Type).HasMaxLength(100);
        }
    }

}
