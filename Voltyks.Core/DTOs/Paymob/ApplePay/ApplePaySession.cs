using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    public class ApplePaySession
    {
        public string MerchantIdentifier { get; set; }
        public string DisplayName { get; set; }
        public string DomainName { get; set; }
        public string Initiative { get; set; } = "web";
        public string InitiativeContext { get; set; }
    }

}
