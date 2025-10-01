using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public class ChargingRequestDetailsDto
    {
        public int RequestId { get; set; }

        // Request Info
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }

        // Car Owner Info
        public string CarOwnerId { get; set; }
        public string CarOwnerName { get; set; }

        // Station Owner Info
        public string StationOwnerId { get; set; }
        public string StationOwnerName { get; set; }

        // Charger Info
        public int ChargerId { get; set; }
        public string Protocol { get; set; }
        public double CapacityKw { get; set; }
        public double KwNeeded { get; set; }
        public int CurrentBatteryPercentage { get; set; }

        public string PricePerHour { get; set; }
        public string AdapterAvailability { get; set; }

        // Charger Location
        public string ChargerArea { get; set; }
        public string ChargerStreet { get; set; }

        // Vehicle Location
        public string VehicleArea { get; set; }
        public string VehicleStreet { get; set; }

        // Extras
        public double EstimatedArrival { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal VoltyksFees { get; set; }


        public double DistanceInKm { get; set; }
        public string VehicleBrand { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleColor { get; set; }
        public string VehiclePlate { get; set; }
        public double VehicleCapacity { get; set; }

    }

}
