using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class VerifyEmailChangeDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = default!;

        [Required]
        [StringLength(4, MinimumLength = 4)]
        public string OtpCode { get; set; } = default!;
    }
}
