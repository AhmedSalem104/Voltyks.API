using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class UpdateProductDto
    {
        public int? CategoryId { get; set; }

        [StringLength(200, MinimumLength = 2)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [StringLength(10)]
        public string? Currency { get; set; }

        public List<string>? Images { get; set; }

        public Dictionary<string, string>? Specifications { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public bool? IsReservable { get; set; }
    }
}
