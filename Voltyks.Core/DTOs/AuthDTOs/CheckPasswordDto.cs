using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class CheckPasswordDto
    {
        [Required]
        public string Password { get; set; } = default!;
    }
}
