using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Persistence.Entities.Main
{
    public class ChargerAddress : BaseEntity<int>
    {
        public string Area { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }

}
