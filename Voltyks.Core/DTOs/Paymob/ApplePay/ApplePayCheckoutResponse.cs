using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    public class ApplePayCheckoutResponse
    {
        public string MerchantOrderId { get; set; }
        public long PaymobOrderId { get; set; }
        public string PaymentKey { get; set; }
        public string PublicKey { get; set; }
        public ApplePaySession Session { get; set; }
    }

}
