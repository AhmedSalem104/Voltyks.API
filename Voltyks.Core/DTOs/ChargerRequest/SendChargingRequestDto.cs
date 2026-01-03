using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.ChargerRequest
{
    public class SendChargingRequestDto
    {
        [Required(ErrorMessage = "Charger ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChargerId must be a positive integer")]
        public int ChargerId { get; set; }

        [Required(ErrorMessage = "KW needed is required")]
        [Range(0.1, 1000.0, ErrorMessage = "KwNeeded must be between 0.1 and 1000")]
        public double KwNeeded { get; set; }

        [Required(ErrorMessage = "Battery percentage is required")]
        [Range(0, 100, ErrorMessage = "Battery percentage must be between 0 and 100")]
        public int CurrentBatteryPercentage { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }
    }
}
