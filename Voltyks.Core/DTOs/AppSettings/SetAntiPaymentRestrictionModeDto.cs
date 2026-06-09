using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AppSettings
{
    public class SetAntiPaymentRestrictionModeDto
    {
        [Required]
        public bool Enabled { get; set; }
    }
}
