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
    }

}
