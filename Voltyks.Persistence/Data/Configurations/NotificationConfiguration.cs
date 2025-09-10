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
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.RelatedRequest)
                   .WithMany()
                   .HasForeignKey(n => n.RelatedRequestId)
                   .OnDelete(DeleteBehavior.SetNull) // المهم
                   .IsRequired(false);

            builder.HasIndex(n => n.RelatedRequestId);
            builder.HasIndex(n => n.UserId);

        }
    }

}
