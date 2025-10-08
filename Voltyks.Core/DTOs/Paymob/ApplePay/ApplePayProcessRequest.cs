using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    public class ApplePayProcessRequest
    {
        public string PaymentToken { get; set; }
        public string PaymentKey { get; set; }
        public string MerchantOrderId { get; set; }
    }

}
