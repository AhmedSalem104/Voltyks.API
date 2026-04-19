using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.VehicleAdditionRequests
{
    public class CreateVehicleAdditionRequestDto
    {
        [Required(ErrorMessage = "Brand name is required")]
        [MaxLength(100)]
        public string BrandName { get; set; } = "";

        [Required(ErrorMessage = "Model name is required")]
        [MaxLength(100)]
        public string ModelName { get; set; } = "";

        [Required(ErrorMessage = "Capacity is required")]
        [MaxLength(50)]
        public string Capacity { get; set; } = "";
    }
}
