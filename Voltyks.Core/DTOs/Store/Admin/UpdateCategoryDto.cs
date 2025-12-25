using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class UpdateCategoryDto
    {
        [StringLength(100, MinimumLength = 2)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Slug { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public int? SortOrder { get; set; }

        [StringLength(255)]
        public string? Icon { get; set; }

        [StringLength(500)]
        public string? PlaceholderMessage { get; set; }
    }
}
