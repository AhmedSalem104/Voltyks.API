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
        public double Rating { get; set; }
        public int RatingCount { get; set; }

        public string Area { get; set; }
        public string Street { get; set; }

        public double DistanceInKm { get; set; }
        public string EstimatedArrival { get; set; }

        public string Protocol { get; set; }
        public int Capacity { get; set; }

        public string PricePerHour { get; set; }
        public string AdapterAvailability { get; set; }

        public string PriceEstimated { get; set; }
    }

}
