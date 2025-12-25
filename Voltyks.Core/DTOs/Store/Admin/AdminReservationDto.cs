using System;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class AdminReservationDto
    {
        public int Id { get; set; }

        // User Info
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }

        // Product Info
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductThumbnail { get; set; }

        // Order Details
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = string.Empty;

        // Payment
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? PaidAt { get; set; }

        // Delivery
        public string DeliveryStatus { get; set; } = string.Empty;
        public DateTime? DeliveredAt { get; set; }
        public string? DeliveryNotes { get; set; }

        // Admin
        public string? AdminNotes { get; set; }
        public DateTime? ContactedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
