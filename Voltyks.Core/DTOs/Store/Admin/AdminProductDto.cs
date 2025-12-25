using System;
using System.Collections.Generic;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class AdminProductDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public List<string> Images { get; set; } = new();
        public Dictionary<string, string>? Specifications { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsReservable { get; set; }
        public int ReservationCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
