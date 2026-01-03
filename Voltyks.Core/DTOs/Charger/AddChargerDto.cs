using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class AddChargerDto
    {
        [Required(ErrorMessage = "Protocol is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ProtocolId must be a positive integer")]
        public int ProtocolId { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "CapacityId must be a positive integer")]
        public int CapacityId { get; set; }

        [Required(ErrorMessage = "Price option is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PriceOptionId must be a positive integer")]
        public int PriceOptionId { get; set; }

        // Address
        [StringLength(100, ErrorMessage = "Area cannot exceed 100 characters")]
        public string Area { get; set; }

        [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters")]
        public string Street { get; set; }

        [StringLength(50, ErrorMessage = "Building number cannot exceed 50 characters")]
        public string BuildingNumber { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        public bool IsActive { get; set; }
        public bool? Adaptor { get; set; }
    }
}
