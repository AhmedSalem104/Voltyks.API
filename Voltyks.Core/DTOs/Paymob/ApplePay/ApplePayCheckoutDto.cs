using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Paymob.Core_API_DTOs;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    public class ApplePayCheckoutDto
    {
        public long AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? MerchantOrderId { get; set; }
        public BillingData? Billing { get; set; }
        public string? DomainName { get; set; }
        public bool SaveCard { get; set; } = false;
    }


}
