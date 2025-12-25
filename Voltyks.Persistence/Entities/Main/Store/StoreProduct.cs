using System;
using System.Collections.Generic;

namespace Voltyks.Persistence.Entities.Main.Store
{
    public class StoreProduct : BaseEntity<int>
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? ImagesJson { get; set; } // JSON array of URLs
        public string? SpecificationsJson { get; set; } // JSON object
        public string Status { get; set; } = "active"; // active, out_of_stock, hidden
        public bool IsReservable { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public StoreCategory? Category { get; set; }
        public ICollection<StoreReservation> Reservations { get; set; } = new List<StoreReservation>();
    }
}
