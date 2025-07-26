using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.VehicleDTOs
{
    public class CreateAndUpdateVehicleDto
    {
        public string Color { get; set; }
        public string Plate { get; set; }

        public int BrandId { get; set; }
        public int ModelId { get; set; }
        public int Year { get; set; }
    }

}
