using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class ChargerDetailsRequestDto
    {
        public int ChargerId { get; set; }
        public double UserLat { get; set; }
        public double UserLon { get; set; }
        public double KwNeed { get; set; }
    }

}
