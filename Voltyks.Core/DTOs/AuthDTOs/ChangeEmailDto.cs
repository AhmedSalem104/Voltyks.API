using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class ChangeEmailDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = default!;

        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string NewEmail { get; set; } = default!;
    }
}
