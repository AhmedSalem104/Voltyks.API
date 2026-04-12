using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AppSettings
{
    public class SetAdminsModeDto
    {
        [Required]
        public bool Activated { get; set; }
    }
}
