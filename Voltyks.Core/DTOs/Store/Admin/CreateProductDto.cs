using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class CreateProductDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "EGP";

        public List<string>? Images { get; set; }

        public Dictionary<string, string>? Specifications { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "active";

        public bool IsReservable { get; set; } = true;
    }
}
