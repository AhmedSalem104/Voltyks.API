using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Voltyks.Core.DTOs.ImageUpload
{
    public class UploadProductImagesDto
    {
        [Required]
        public List<IFormFile> Images { get; set; } = new();
    }
}
