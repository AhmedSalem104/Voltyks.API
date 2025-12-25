using System;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class AdminCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string? Icon { get; set; }
        public string? PlaceholderMessage { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
