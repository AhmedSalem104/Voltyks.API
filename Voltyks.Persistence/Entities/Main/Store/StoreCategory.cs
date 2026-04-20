using System;
using System.Collections.Generic;
using Voltyks.Persistence.Utilities;

namespace Voltyks.Persistence.Entities.Main.Store
{
    public class StoreCategory : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = "active"; // active, coming_soon, hidden
        public int SortOrder { get; set; } = 0;
        public string? Icon { get; set; }
        public string? PlaceholderMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTimeHelper.GetEgyptTime();
        public DateTime UpdatedAt { get; set; } = DateTimeHelper.GetEgyptTime();
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public ICollection<StoreProduct> Products { get; set; } = new List<StoreProduct>();
    }
}
