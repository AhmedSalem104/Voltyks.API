using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.Core_API_DTOs
{
   public record PaymobPaymentKeyReq(
   string auth_token, long amount_cents, int expiration,
   long order_id, object billing_data, string currency, int integration_id,
   bool? tokenize = null
);

}
