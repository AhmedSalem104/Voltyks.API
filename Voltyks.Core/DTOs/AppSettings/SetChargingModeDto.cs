using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AppSettings
{
    public class SetChargingModeDto
    {
        [Required]
        public bool Enabled { get; set; }
    }
}
