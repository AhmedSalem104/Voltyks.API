using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public class SendChargingRequestDto
    {
        public int ChargerId { get; set; }
        public double KwNeeded { get; set; }
        public int CurrentBatteryPercentage { get; set; }
        public double Latitude { get; set; }   
        public double Longitude { get; set; }     


        // public string? DeviceToken { get; set; } // ✅ أضف دي

    }

}
