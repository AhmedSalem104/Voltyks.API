using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.ImageUpload
{
    public class DeleteProductImageDto
    {
        [Required]
        public string ImagePath { get; set; } = string.Empty;
    }
}
