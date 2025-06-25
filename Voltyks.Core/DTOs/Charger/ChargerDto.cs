using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class ChargerDto
    {
        public int Id { get; set; }
        public string Protocol { get; set; }
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public string BuildingNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateAdded { get; set; }
    }

}
