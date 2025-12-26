using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class RecordPaymentDto
    {
        [StringLength(50)]
        public string? PaymentMethod { get; set; } // optional - default: "manual"

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        public decimal? PaidAmount { get; set; } // optional - default: TotalPrice

        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }
}
