using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class ChargerDetailsDto
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }

        public double Rating { get; set; }
        public int RatingCount { get; set; }

        public string Area { get; set; }
        public string Street { get; set; }

        public string BuildingNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double DistanceInKm { get; set; }
        public double EstimatedArrival { get; set; }

        public string Protocol { get; set; }
        public CapacityDto Capacity { get; set; }


        public decimal PricePerHour { get; set; }
        public string AdapterAvailability { get; set; }

        public double PriceEstimated { get; set; }

        /// <summary>
        /// Estimated charging time in hours based on KwNeed and charger capacity
        /// </summary>
        public double TimeNeeded { get; set; }

        /// <summary>
        /// The amount of KW needed for charging
        /// </summary>
        public double KwNeeded { get; set; }
    }

}
