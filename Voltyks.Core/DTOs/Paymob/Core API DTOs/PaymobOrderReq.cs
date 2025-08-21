using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
    public record PaymobOrderReq(string auth_token, long amount_cents, string currency,
                              string merchant_order_id, object[] items);
}
