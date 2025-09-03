using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.AddtionDTOs
{
    public class WalletCheckoutRequest
    {

        public string? MerchantOrderId { get; set; }   // اختياري
        public long AmountCents { get; set; }
        public string Currency { get; set; } = "EGP";  // قيمة افتراضية
        public string WalletPhone { get; set; } = string.Empty;
    }

}
