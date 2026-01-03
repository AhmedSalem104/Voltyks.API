using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.VehicleDTOs
{
    public class CreateAndUpdateVehicleDto
    {
        [Required(ErrorMessage = "Color is required")]
        [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Plate number is required")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Plate must be between 2 and 20 characters")]
        public string Plate { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        [Range(1, int.MaxValue, ErrorMessage = "BrandId must be a positive integer")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Model is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ModelId must be a positive integer")]
        public int ModelId { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
        public int Year { get; set; }
    }
}
