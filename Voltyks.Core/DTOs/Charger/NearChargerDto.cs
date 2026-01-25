using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class NearChargerDto
    {
        public int ChargerId { get; set; }
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public double DistanceInKm { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool? AdapterAvailable { get; set; }  // معلومة إضافية فقط
    }

}
