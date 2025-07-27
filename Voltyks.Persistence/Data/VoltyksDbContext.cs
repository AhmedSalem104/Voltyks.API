using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Voltyks.Persistence.Entities;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;

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

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Capacity> Capacities { get; set; }
        public DbSet<Protocol> Protocols { get; set; }
        public DbSet<PriceOption> PriceOptions { get; set; }
        public DbSet<Charger> Chargers { get; set; }
        public DbSet<ChargingRequest> ChargingRequests { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }



    }
}
