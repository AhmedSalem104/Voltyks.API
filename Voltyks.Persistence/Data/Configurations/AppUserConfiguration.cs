using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Data.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            // Performance Indexes for common search patterns
            builder.HasIndex(u => new { u.Email, u.IsBanned })
                   .HasDatabaseName("IX_AspNetUsers_Email_IsBanned");

            builder.HasIndex(u => new { u.PhoneNumber, u.IsBanned })
                   .HasDatabaseName("IX_AspNetUsers_PhoneNumber_IsBanned");

            builder.HasIndex(u => u.IsBanned)
                   .HasDatabaseName("IX_AspNetUsers_IsBanned");

            builder.HasIndex(u => u.IsAvailable)
                   .HasDatabaseName("IX_AspNetUsers_IsAvailable");
        }
    }
}
