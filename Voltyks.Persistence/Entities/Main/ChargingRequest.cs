using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main
{
    public class ChargingRequest:BaseEntity<int>
    {

        public string UserId { get; set; }
        public AppUser CarOwner { get; set; }
        public double KwNeeded { get; set; }
        public int CurrentBatteryPercentage { get; set; }
        public double Latitude { get; set; }     
        public double Longitude { get; set; }
        public string RecipientUserId { get; set; }
        public int ChargerId { get; set; }
        public Charger Charger { get; set; }

        public string Status { get; set; } // pending / accepted / rejected / confirmed / expired
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }


        public decimal BaseAmount { get; set; }      // المبلغ الأساسي (للشاحن)
        public decimal VoltyksFees { get; set; }      // رسوم Voltyks
        public decimal EstimatedPrice { get; set; }  // المجموع الكلي (Base + Fee)

    }

}
