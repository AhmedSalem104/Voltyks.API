using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class RequestEmailChangeDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = default!;
    }
}
