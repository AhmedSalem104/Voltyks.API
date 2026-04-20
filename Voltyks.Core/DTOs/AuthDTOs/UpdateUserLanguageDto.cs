using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.AuthDTOs
{
    public class UpdateUserLanguageDto
    {
        /// <summary>Language code: "en" or "ar" (case-insensitive). Anything else falls back to "en".</summary>
        [Required(ErrorMessage = "Language is required")]
        public string Language { get; set; } = "";
    }
}
