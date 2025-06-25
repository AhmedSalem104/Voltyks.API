using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class PriceByCapacityDto
    {
        public int Capacity { get; set; }
        public List<decimal> AvailablePrices { get; set; }
    }

}
