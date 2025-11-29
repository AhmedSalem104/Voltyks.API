using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Complaints
{
    public class CreateGeneralComplaintDto
    {
        /// <summary>
        /// Optional: If provided, complaint will be created for this user.
        /// If null/empty, uses the currently authenticated user.
        /// </summary>
        public string? UserId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = default!;
    }
}
