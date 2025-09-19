using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;
using Voltyks.Persistence.Entities;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public class CardCheckoutRequest
    {

        [Range(1, long.MaxValue, ErrorMessage = "amountCents must be > 0")]
        public int AmountCents { get; set; }

        [Required] 
        public BillingDataInitDto Billing { get; set; } = default!;

        [Required]
        public string PaymentMethod { get; set; } = "Card";
    }

}
