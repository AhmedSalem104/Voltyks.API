using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Persistence.Entities;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public class CardCheckoutServiceDto
    {
        public string? MerchantOrderId { get; set; }   // اختياري

        [Range(1, long.MaxValue, ErrorMessage = "amountCents must be > 0")]
        public long AmountCents { get; set; }
        public bool SaveCard { get; set; } // اختياري، false افتراضيًا


        public string? Currency { get; set; }

        [Required]
        public BillingData Billing { get; set; } = default!;
    }

}
