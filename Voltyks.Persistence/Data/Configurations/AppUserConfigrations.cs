using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Data.Configurations
{
    public class AppUserConfigurations : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.HasOne(u => u.Address)
                   .WithOne(a => a.AppUser)
                   .HasForeignKey<Address>(a => a.AppUserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
