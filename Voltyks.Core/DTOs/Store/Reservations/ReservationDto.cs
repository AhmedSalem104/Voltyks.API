using System;

namespace Voltyks.Core.DTOs.Store.Reservations
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductThumbnail { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "EGP";
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
