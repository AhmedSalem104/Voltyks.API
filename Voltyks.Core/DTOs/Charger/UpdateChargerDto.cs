using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class UpdateChargerDto
    {
        //public int ChargerId { get; set; }
        public int ProtocolId { get; set; }
        public int CapacityId { get; set; }
        public int PriceOptionId { get; set; }

        public string Area { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool? Adaptor { get; set; }
        public bool IsActive { get; set; }
    }

}
