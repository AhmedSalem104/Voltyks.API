using System;
using Voltyks.Persistence.Entities.Identity;

namespace Voltyks.Persistence.Entities.Main.Store
{
    public class StoreReservation : BaseEntity<int>
    {
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "pending"; // pending, contacted, completed, cancelled

        // Payment tracking
        public string PaymentStatus { get; set; } = "unpaid"; // unpaid, paid
        public string? PaymentMethod { get; set; } // cash, bank_transfer, instapay, vodafone_cash, fawry, other
        public string? PaymentReference { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? PaidAt { get; set; }

        // Delivery tracking
        public string DeliveryStatus { get; set; } = "pending"; // pending, delivered
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }

        // Admin
        public string? AdminNotes { get; set; }
        public DateTime? ContactedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser? User { get; set; }
        public StoreProduct? Product { get; set; }
    }
}
