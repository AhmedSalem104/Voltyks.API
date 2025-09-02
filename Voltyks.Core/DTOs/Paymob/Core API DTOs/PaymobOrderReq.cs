using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    public class PaymobOrderReq
    {
        public string auth_token { get; set; }
        public long amount_cents { get; set; }

        public string currency { get; set; }

        public string merchant_order_id { get; set; }

    }
       
 
}
