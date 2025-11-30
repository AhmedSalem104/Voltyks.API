using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Data.Configurations
{
    public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
    {
        public void Configure(EntityTypeBuilder<DeviceToken> builder)
        {
            builder.HasOne(t => t.User)
                   .WithMany(u => u.DeviceTokens)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Performance Indexes
            builder.HasIndex(t => t.UserId)
                   .HasDatabaseName("IX_DeviceTokens_UserId");

            builder.HasIndex(t => t.Token)
                   .HasDatabaseName("IX_DeviceTokens_Token");
        }
    }

}
