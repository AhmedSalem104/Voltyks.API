using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.CardsDTOs
{
    public record CardPaymentKeyRes(
     string MerchantOrderId,
     long PaymobOrderId,
     string PaymentKey,
     string IframeUrl
 );

}
