using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Complaints
{
    public class CreateGeneralComplaintDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = default!;
    }
}
