using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Voltyks.Persistence.Entities.Main;

namespace Voltyks.Persistence.Entities.Identity
{
    public class AppUser : IdentityUser 
    {
        public string? FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; } 
        public string? NationalId { get; set; }
        public string? RefreshToken { get; set; }
        public int? Vcode { get; set; }
        public DateTime? VcodeExpirationDate { get; set; }

        public Address Address { get; set; } = new Address();  // تهيئة افتراضية

        public bool IsAvailable { get; set; } = true;

        public bool IsBanned { get; set; } = false;

        public double Wallet { get; set; } = 0;


        public string? CurrentActivitiesJson { get; set; } = "[]";


        [NotMapped]
        public ICollection<int> CurrentActivities
        {
            get => string.IsNullOrWhiteSpace(CurrentActivitiesJson)
                    ? new List<int>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<int>>(CurrentActivitiesJson) ?? new List<int>();
            set => CurrentActivitiesJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<int>());
        }

        public bool UserShouldBeBanned { get; set; } = false;

        public double Rating { get; set; } = 0;
        public int RatingCount { get; set; } = 0;
        // Relations
        public ICollection<Charger> Chargers { get; set; } = new List<Charger>();
        public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();
        public ICollection<ChargingRequest> ChargingRequests { get; set; } = new List<ChargingRequest>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    }
}
