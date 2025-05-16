using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Data
{
    public class VoltyksDbContext : IdentityDbContext<AppUser>
    {
        public VoltyksDbContext(DbContextOptions<VoltyksDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        // public DbSet<Product> Products { get; set; }
    }
}
