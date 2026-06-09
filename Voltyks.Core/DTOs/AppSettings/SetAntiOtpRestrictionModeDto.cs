using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AppSettings
{
    public class SetAntiOtpRestrictionModeDto
    {
        [Required]
        public bool Enabled { get; set; }
    }
}
