using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    public class ApplePayValidationResponse
    {
        public string SessionId { get; set; }
        public string MerchantSession { get; set; }
        public bool IsValid { get; set; }
    }


}
