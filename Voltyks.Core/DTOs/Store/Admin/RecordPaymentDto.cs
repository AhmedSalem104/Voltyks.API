using System.ComponentModel.DataAnnotations;

namespace Voltyks.Core.DTOs.Store.Admin
{
    public class RecordPaymentDto
    {
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // cash, bank_transfer, instapay, vodafone_cash, fawry, other

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Paid amount must be greater than 0")]
        public decimal PaidAmount { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }
}
