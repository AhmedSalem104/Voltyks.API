using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voltyks.Persistence.Entities;
using Voltyks.Persistence.Entities.Identity;
using Voltyks.Persistence.Entities.Main;
using Voltyks.Persistence.Entities.Main.Paymob;
using Voltyks.Persistence.Entities.Main.Store;

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
        public DbSet<UserType> UserTypes { get; set; }

        // Paymob
        public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();
        public DbSet<PaymentAction> PaymentActions => Set<PaymentAction>();
        public DbSet<UserSavedCard> UserSavedCards => Set<UserSavedCard>();
        public DbSet<CardTokenWebhookLog> CardTokenWebhookLogs => Set<CardTokenWebhookLog>();




        public DbSet<FeesConfig> feesConfigs { get; set; }
        public DbSet<TermsDocument>  termsDocuments{ get; set; }
        public DbSet<RatingsHistory> RatingsHistory { get; set; }
        public DbSet<Process> Process { get; set; }

        public DbSet<UserReport> UserReports { get; set; }


        public DbSet<UsersBanned> UsersBanneds { get; set; }

        // Complaint System
        public DbSet<ComplaintCategory> ComplaintCategories { get; set; }
        public DbSet<UserGeneralComplaint> UserGeneralComplaints { get; set; }

        // Wallet Transactions
        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        // Mobile App Config
        public DbSet<MobileAppConfig> MobileAppConfigs { get; set; }

        // App Settings
        public DbSet<AppSettings> AppSettings { get; set; }

        // Store Module
        public DbSet<StoreCategory> StoreCategories { get; set; }
        public DbSet<StoreProduct> StoreProducts { get; set; }
        public DbSet<StoreReservation> StoreReservations { get; set; }

    }
}
