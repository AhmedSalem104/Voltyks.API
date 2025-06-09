using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.VehicleDTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string Color { get; set; }
        public string Plate { get; set; }
        public DateTime CreationDate { get; set; }

        public int Year { get; set; }

        public int BrandId { get; set; }
        public string BrandName { get; set; }

        public int ModelId { get; set; }
        public string ModelName { get; set; }
    }
}
