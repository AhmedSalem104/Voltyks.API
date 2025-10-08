using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.ApplePay
{
    public class ApplePayValidationRequest
    {
        public string ValidationUrl { get; set; }
        public string DisplayName { get; set; }
        public string DomainName { get; set; }
    }

}
